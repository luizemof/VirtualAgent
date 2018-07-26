using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent.LUIS.ActionBinding.Action
{
    public interface ILuisContextualAction<T> : ILuisAction where T : ILuisAction
    {
        T Context { get; set; }
    }
}
