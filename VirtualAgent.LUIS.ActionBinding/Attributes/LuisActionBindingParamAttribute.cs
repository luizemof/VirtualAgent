using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent.LUIS.ActionBinding.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class LuisActionBindingParamAttribute : Attribute
    {
        public string CustomType { get; set; }

        public string BuiltinType { get; set; }

        public int Order { get; set; }
    }
}
