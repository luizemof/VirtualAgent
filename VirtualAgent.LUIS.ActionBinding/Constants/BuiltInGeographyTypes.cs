using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAgent.LUIS.ActionBinding.Constants
{
    internal class BuiltInGeographyTypes
    {
        private const string City = "builtin.geography.city";
        private const string Country = "builtin.geography.country";
        private const string PointOfInterest = "builtin.geography.pointOfInterest";

        public string CityType => City;
        public string CountryType => Country;
        public string PointOfInterestType => PointOfInterest;
    }
}
