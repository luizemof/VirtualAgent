using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VirtualAgent.LUIS.ActionBinding.Action;
using VirtualAgent.LUIS.ActionBinding.Attributes;

namespace VirtualAgent.LUIS.ActionBinding.Dialog
{
	#region Delegates
	public delegate Task LuisActionHandler(IDialogContext context, object actionResult);
	public delegate Task LuisActionActivityHandler(IDialogContext context, IAwaitable<IMessageActivity> message, object actionResult);
	#endregion

	[Serializable]
	public class LuisActionDialog<TResult> : LuisDialog<TResult>
	{
        private readonly LuisActionResolver actionResolver;
        private readonly Action<ILuisAction, object> onContextCreation;

        public LuisActionDialog(IEnumerable<Assembly> assemblies, params ILuisService[] services)
            : this(assemblies, null, services) { }

        public LuisActionDialog(
            IEnumerable<Assembly> assemblies,
            Action<ILuisAction, object> onContextCreation,
            params ILuisService[] services) : base(services)
        {
            if (assemblies == null)
                throw new ArgumentException(nameof(Assembly));

            this.onContextCreation = onContextCreation;
            this.actionResolver = new LuisActionResolver(assemblies.ToArray());
        }

        protected override async Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            LuisServiceResult winner = await GetWinner(context, item);
            if (winner == null)
                throw new InvalidOperationException("No winning intent selected from Luis results.");

            ILuisAction luisAction = this.actionResolver.ResolveActionFromLuisIntent(winner.Result, out string intentName);
            if (luisAction != null)
            {
                var executionContextChain = new ActionExecutionContext(intentName, luisAction).ToSingleList();
                while (LuisActionResolver.IsContextualAction(luisAction))
                {
                    if (!LuisActionResolver.CanStartWithNoContextAction(luisAction, out LuisActionBindingAttribute luisActionDefinition))
                    {
                        await context.PostAsync($"Cannot start contextual action '{luisActionDefinition.FriendlyName}' without a valid context.");
                        return;
                    }

                    luisAction = LuisActionResolver.BuildContextForContextualAction(luisAction, out intentName);
                    if (luisAction != null)
                    {
                        this.onContextCreation?.Invoke(luisAction, context);
                        executionContextChain.Insert(0, new ActionExecutionContext(intentName, luisAction));
                    }
                }

                if (!luisAction.IsValid(out ICollection<ValidationResult> validationResults))
                {
                    var childDialog = new LuisActionMissingEntitiesDialog(winner.LuisService, executionContextChain);
                    context.Call(childDialog, this.LuisActionMissingDialogFinished);
                }
                else
                {
                    await this.DispatchToLuisActionActivityHandler(context, item, intentName, luisAction);
                }
            }
        }

        protected virtual async Task LuisActionMissingDialogFinished(IDialogContext context, IAwaitable<ActionExecutionContext> executionContext)
        {
            var messageActivity = (IMessageActivity)context.Activity;
            var executionContextResult = await executionContext;
            await this.DispatchToLuisActionActivityHandler(context, Awaitable.FromItem(messageActivity), executionContextResult.Intent, executionContextResult.Action);
        }

        protected virtual async Task DispatchToLuisActionActivityHandler(IDialogContext context, IAwaitable<IMessageActivity> item, string intentName, ILuisAction luisAction)
        {
            var actionHandlerByIntent = new Dictionary<string, LuisActionActivityHandler>(this.GetActionHandlersByIntent());

            if (!actionHandlerByIntent.TryGetValue(intentName, out LuisActionActivityHandler handler))
                handler = actionHandlerByIntent[string.Empty];

            if (handler != null)
                await handler(context, item, await luisAction.FulfillAsync());
            else
                throw new Exception($"No default intent handler found.");
        }

        protected virtual IDictionary<string, LuisActionActivityHandler> GetActionHandlersByIntent()
        {
            return LuisActionDialogHelper.EnumerateHandlers(this).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private async Task<LuisServiceResult> GetWinner(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            IMessageActivity message = await item;
            var messageText = await GetLuisQueryTextAsync(context, message);
            var tasks = this.services.Select(s => s.QueryAsync(messageText, context.CancellationToken)).ToArray();
            var results = await Task.WhenAll(tasks);

            var winners = from result in results.Select((value, index) => new { value, index })
                          let resultWinner = this.BestIntentFrom(result.value)
                          where resultWinner != null
                          select new LuisServiceResult(result.value, resultWinner, this.services[result.index]);

            var winner = this.BestResultFrom(winners);
            return winner;
        }
    }
}
