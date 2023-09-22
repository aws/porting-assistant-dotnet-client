using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Compatibility.Common.Model.Exception
{
    public class PackageNotFoundException : System.Exception
    {
        public PackageNotFoundException(string message) :
            base(message)
        {
        }

        public PackageNotFoundException(PackageVersionPair packageVersion) :
            base(DefaultMessage(packageVersion))
        {
        }

        private static string DefaultMessage(PackageVersionPair packageVersion)
        {
            return $"Could not find package {packageVersion}.";
        }
    }
}
