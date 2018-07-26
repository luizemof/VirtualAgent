using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent.LUIS.ActionBinding.Action
{
    [Serializable]
    internal class LuisActionMissingEntitiesDialog : IDialog<ActionExecutionContext>
    {
        private string intentName;
        private readonly ILuisService luisService;
        private ILuisAction luisAction;
        private IList<ActionExecutionContext> executionContextChain;
        private QueryValueResult overrunData;

        public LuisActionMissingEntitiesDialog(ILuisService luisService, IList<ActionExecutionContext> executionContextChain)
        {
            if (executionContextChain == null || executionContextChain.Count == 0)
            {
                throw new ArgumentException("Action chain cannot be null or empty.", nameof(executionContextChain));
            }

            var executionContext = executionContextChain.First();

            SetField.NotNull(out this.luisService, nameof(luisService), luisService);
            SetField.NotNull(out this.intentName, nameof(this.intentName), executionContext.Intent);
            SetField.NotNull(out this.luisAction, nameof(this.luisAction), executionContext.Action);

            executionContextChain.RemoveAt(0);
            if (executionContextChain.Count > 0)
            {
                this.executionContextChain = executionContextChain;
            }
        }

        public virtual async Task StartAsync(IDialogContext context)
        {
            if (this.executionContextChain != null)
            {
                var childDialog = new LuisActionMissingEntitiesDialog(this.luisService, this.executionContextChain);

                // clean executionContextChain - avoid serialization payload
                this.executionContextChain = null;

                context.Call(childDialog, this.AfterContextualActionFinished);

                return;
            }

            await this.MessageReceivedAsync(context, null);
        }

        protected virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var nextPromptIdx = 0;
            this.luisAction.IsValid(out ICollection<ValidationResult> validationResults);
            if (item != null)
            {
                var message = await item;
                var paramName = validationResults.First().MemberNames.First();
                var paramValue = message.Text;

                var result = await LuisActionResolver.QueryValueFromLuisAsync(this.luisService, this.luisAction, paramName, paramValue, context.CancellationToken);

                if (result.Succeed)
                {
                    nextPromptIdx++;
                }
                else if (!string.IsNullOrWhiteSpace(result.NewIntent) && result.NewAction != null)
                {
                    var currentActionDefinition = LuisActionResolver.GetActionDefinition(this.luisAction);

                    var isContextual = false;
                    if (LuisActionResolver.IsValidContextualAction(result.NewAction, this.luisAction, out isContextual))
                    {
                        var executionContextChain = new List<ActionExecutionContext> { new ActionExecutionContext(result.NewIntent, result.NewAction) };

                        var childDialog = new LuisActionMissingEntitiesDialog(this.luisService, executionContextChain);

                        context.Call(childDialog, this.AfterContextualActionFinished);

                        return;
                    }
                    else if (isContextual & !LuisActionResolver.IsContextualAction(this.luisAction))
                    {
                        var newActionDefinition = LuisActionResolver.GetActionDefinition(result.NewAction);

                        await context.PostAsync($"Cannot execute action '{newActionDefinition.FriendlyName}' in the context of '{currentActionDefinition.FriendlyName}' - continuing with current action");
                    }
                    else if (!this.luisAction.GetType().Equals(result.NewAction.GetType()))
                    {
                        var newActionDefinition = LuisActionResolver.GetActionDefinition(result.NewAction);

                        var valid = LuisActionResolver.UpdateIfValidContextualAction(result.NewAction, this.luisAction, out isContextual);
                        if (!valid && isContextual)
                        {
                            await context.PostAsync($"Cannot switch to action '{newActionDefinition.FriendlyName}' from '{currentActionDefinition.FriendlyName}' due to invalid context - continuing with current action");
                        }
                        else if (currentActionDefinition.ConfirmOnSwitchingContext)
                        {
                            // serialize overrun info
                            this.overrunData = result;

                            PromptDialog.Confirm(
                                context,
                                this.AfterOverrunCurrentActionSelected,
                                $"Do you want to discard the current action '{currentActionDefinition.FriendlyName}' and start executing '{newActionDefinition.FriendlyName}' action?");

                            return;
                        }
                        else
                        {
                            this.intentName = result.NewIntent;
                            this.luisAction = result.NewAction;

                            this.luisAction.IsValid(out validationResults);
                        }
                    }
                }
            }

            if (validationResults.Count > nextPromptIdx)
            {
                await context.PostAsync(validationResults.ElementAt(nextPromptIdx).ErrorMessage);
                context.Wait(this.MessageReceivedAsync);
            }
            else
            {
                context.Done(new ActionExecutionContext(this.intentName, this.luisAction));
            }
        }

        private async Task AfterOverrunCurrentActionSelected(IDialogContext context, IAwaitable<bool> result)
        {
            if (await result == true)
            {
                // if switching from contextual to other root
                if (LuisActionResolver.IsContextualAction(this.luisAction) && !LuisActionResolver.IsContextualAction(this.overrunData.NewAction))
                {
                    context.Done(new ActionExecutionContext(this.overrunData.NewIntent, this.overrunData.NewAction) { ChangeRootSignaling = true });

                    return;
                }

                this.intentName = this.overrunData.NewIntent;
                this.luisAction = this.overrunData.NewAction;
            }

            // clean overrunData - avoid serialization payload
            this.overrunData = null;

            await this.MessageReceivedAsync(context, null);
        }

        private async Task AfterContextualActionFinished(IDialogContext context, IAwaitable<ActionExecutionContext> executionContext)
        {
            var executionContextResult = await executionContext;

            if (executionContextResult.ChangeRootSignaling)
            {
                if (LuisActionResolver.IsContextualAction(this.luisAction))
                {
                    context.Done(executionContextResult);

                    return;
                }
                else
                {
                    this.intentName = executionContextResult.Intent;
                    this.luisAction = executionContextResult.Action;
                }
            }
            else
            {
                var result = await executionContextResult.Action.FulfillAsync();
                if (result is string)
                {
                    await context.PostAsync(result.ToString());
                }
            }

            await this.MessageReceivedAsync(context, null);
        }
    }
}
