using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VirtualAgent.Core.Actions.Greeting;
using VirtualAgent.LUIS.ActionBinding.Constants;
using VirtualAgent.LUIS.ActionBinding.Dialog;

namespace VirtualAgent.Core.Dialogs
{
    [Serializable]
    public class LuisDialog : LuisActionDialog<object>
    {
        public LuisDialog(string luisModelId, string luisSubscriptionKey) : base(
            assemblies: new Assembly[] { typeof(GreetingAction).Assembly },
            onContextCreation: null,
            services: CreateService(luisModelId, luisSubscriptionKey))
        { }

        private static LuisService CreateService(string luisModelId, string luisSubscriptionKey)
        {
            LuisModelAttribute attribute = new LuisModelAttribute(luisModelId, luisSubscriptionKey);
            return new LuisService(attribute);
        }

        [LuisIntent(Intents.Greeting)]
        public async Task GreetingActionHandler(IDialogContext context, object actionResult)
        {
            IMessageActivity message = context.MakeMessage();
            message.Text = actionResult != null ? actionResult.ToString() : "Cannot resolve your query";
            await context.PostAsync(message);
        }

        [LuisIntent(Intents.None)]
        public async Task NoneActionHandler(IDialogContext context, object actionResult)
        {
            var message = context.MakeMessage();
            message.Text = actionResult != null ? actionResult.ToString() : "Cannot resolve your query";
            await context.PostAsync(message);
        }
    }
}
