using System;
using System.Threading.Tasks;
using VirtualAgent.LUIS.ActionBinding.Action;
using VirtualAgent.LUIS.ActionBinding.Attributes;
using VirtualAgent.LUIS.ActionBinding.Constants;

namespace VirtualAgent.Core.Actions.None
{
    [Serializable]
    [LuisActionBinding(Intents.None)]
    public class NoneAction : BaseLuisAction
    {
        public override Task<object> FulfillAsync()
        {
            return Task.FromResult((object)$"Sorry, I didn't understand");
        }
    }
}
