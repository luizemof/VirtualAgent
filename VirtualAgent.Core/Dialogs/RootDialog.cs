using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Configuration;
using System.Threading.Tasks;
using VirtualAgent.Core.WebApiContracts.Authentication;

namespace VirtualAgent.Core.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog
    {
        private const string LUIS_MODEL_ID = "LUIS_MODEL_ID";
        private const string LUIS_SUBSCRIPTION_KEY = "LUIS_SUBSCRIPTION_KEY";

        AuthenticationResult Result;
        public RootDialog(AuthenticationResult result)
        {
            Result = result;
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        private Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            switch (Result.Type)
            {
                case AuthenticationResultType.OK:
                    context.Call(new LuisDialog(ConfigurationManager.AppSettings[LUIS_MODEL_ID], ConfigurationManager.AppSettings[LUIS_SUBSCRIPTION_KEY]), LuisAfterResume);
                    break;
                case AuthenticationResultType.AskedForAuth:
                    context.Call(new AuthenticationDialog(Result.Result), AuthenticationAfterResume);
                    break;
                case AuthenticationResultType.Error:
                    break;
                default:
                    break;
            }
            return Task.CompletedTask;
        }

        private Task LuisAfterResume(IDialogContext context, IAwaitable<object> result)
        {
            return Task.CompletedTask;
        }

        private Task AuthenticationAfterResume(IDialogContext context, IAwaitable<object> result)
        {
            return Task.CompletedTask;
        }
    }
}
