using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent.LUIS.ActionBinding.Constants
{
    internal static class BuiltInTypes
    {
        private const string Age = "builtin.age";
        private const string Dimension = "builtin.dimension";
        private const string Email = "builtin.email";
        private const string Money = "builtin.money";
        private const string Number = "builtin.number";
        private const string Ordinal = "builtin.ordinal";
        private const string Percentage = "builtin.percentage";
        private const string Phonenumber = "builtin.phonenumber";
        private const string Temperature = "builtin.temperature";
        private const string Url = "builtin.url";
        private const string SharedPrefix = "builtin.";

        public static BuiltInDatetimeTypes Datetime { get; } = new BuiltInDatetimeTypes();
        public static BuiltInEncyclopediaTypes Encyclopedia { get; } = new BuiltInEncyclopediaTypes();
        public static BuiltInGeographyTypes Geography { get; } = new BuiltInGeographyTypes();
        public static bool IsBuiltInType(string type)
        {
            return type.StartsWith(SharedPrefix, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
