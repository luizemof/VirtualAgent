using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent.LUIS.ActionBinding.Constants
{
    internal class BuiltInDatetimeTypes
    {
        private const string Date = "builtin.datetime.date";
        private const string Time = "builtin.datetime.time";
        private const string Duration = "builtin.datetime.duration";
        private const string Set = "builtin.datetime.set";

        public string DateType => Date;
        public string DurationType => Duration;
        public string SetType => Set;
        public string TimeType => Time;
    }
}
