using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent.LUIS.ActionBinding.Action
{
    public interface ILuisAction
    {
        Task<object> FulfillAsync();
        bool IsValid(out ICollection<ValidationResult> results);
    }
}
