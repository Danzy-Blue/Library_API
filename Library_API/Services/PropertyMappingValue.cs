using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library_API.Services
{
    public class PropertyMappingValue
    {
        public IEnumerable<string> DestinationProperties { get; private set; }

        public bool Revert { get; private set; }
        public PropertyMappingValue(IEnumerable<string> destinationProperties, bool revert =false)
        {
            this.DestinationProperties = destinationProperties;
            this.Revert = revert;

        }
    }
}
