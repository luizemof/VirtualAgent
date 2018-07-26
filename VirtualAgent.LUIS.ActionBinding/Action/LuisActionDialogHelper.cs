using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VirtualAgent.LUIS.ActionBinding.Dialog;

namespace VirtualAgent.LUIS.ActionBinding.Action
{
    internal class LuisActionDialogHelper
    {
        public static IEnumerable<KeyValuePair<string, LuisActionActivityHandler>> EnumerateHandlers(object dialog)
        {
            var type = dialog.GetType();
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var method in methods)
            {
                var intents = method.GetCustomAttributes<LuisIntentAttribute>(inherit: true).ToArray();
                LuisActionActivityHandler intentHandler = null;

                try
                {
                    intentHandler = (LuisActionActivityHandler)Delegate.CreateDelegate(typeof(LuisActionActivityHandler), dialog, method, throwOnBindFailure: false);
                }
                catch (ArgumentException)
                {
                    // "Cannot bind to the target method because its signature or security transparency is not compatible with that of the delegate type."
                    // https://github.com/Microsoft/BotBuilder/issues/634
                    // https://github.com/Microsoft/BotBuilder/issues/435
                }

                // fall back for compatibility
                if (intentHandler == null)
                {
                    try
                    {
                        var handler = (LuisActionHandler)Delegate.CreateDelegate(typeof(LuisActionHandler), dialog, method, throwOnBindFailure: false);

                        if (handler != null)
                        {
                            // thunk from new to old delegate type
                            intentHandler = (context, message, result) => handler(context, result);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // "Cannot bind to the target method because its signature or security transparency is not compatible with that of the delegate type."
                        // https://github.com/Microsoft/BotBuilder/issues/634
                        // https://github.com/Microsoft/BotBuilder/issues/435
                    }
                }

                if (intentHandler != null)
                {
                    var intentNames = intents.Select(i => i.IntentName).DefaultIfEmpty(method.Name);

                    foreach (var intentName in intentNames)
                    {
                        var key = string.IsNullOrWhiteSpace(intentName) ? string.Empty : intentName;
                        yield return new KeyValuePair<string, LuisActionActivityHandler>(intentName, intentHandler);
                    }
                }
                else
                {
                    if (intents.Length > 0)
                    {
                        var msg = $"Handler '{method.Name}' signature is not valid for the following intent/s: {string.Join(";", intents.Select(i => i.IntentName))}";
                        throw new InvalidIntentHandlerException(msg, method);
                    }
                }
            }
        }
    }
}
