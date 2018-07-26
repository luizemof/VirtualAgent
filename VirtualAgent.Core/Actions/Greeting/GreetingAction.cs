using System;
using System.Threading.Tasks;
using VirtualAgent.LUIS.ActionBinding.Action;
using VirtualAgent.LUIS.ActionBinding.Attributes;
using VirtualAgent.LUIS.ActionBinding.Constants;

namespace VirtualAgent.Core.Actions.Greeting
{
    [Serializable]
    [LuisActionBinding(Intents.Greeting)]
    public class GreetingAction : BaseLuisAction
    {
        public override Task<object> FulfillAsync()
        {
            return Task.FromResult((object)$"Hi! How can I help you?");
        }
    }
}
