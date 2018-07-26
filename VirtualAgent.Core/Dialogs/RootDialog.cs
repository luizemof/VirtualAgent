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
    public class RootDialog : IDialog
    {

        public RootDialog()
        {

        }

        public Task StartAsync(IDialogContext context)
        {
            throw new NotImplementedException();
        }
    }
}
