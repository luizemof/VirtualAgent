using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent.LUIS.ActionBinding.Constants
{
    internal class BuiltInEncyclopediaTypes
    {
        private const string Person = "builtin.encyclopedia.people.person";
        private const string Organization = "builtin.encyclopedia.organization.organization";
        private const string Event = "builtin.encyclopedia.time.event";

        public string PersonType => Person;
        public string OrganizationType => Organization;
        public string EventType => Event;
    }
}
