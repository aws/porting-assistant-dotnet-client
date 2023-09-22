using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Compatibility.Common.Model.Exception
{
    public class PackageDownloadMismatchException : System.Exception
    {

        public PackageDownloadMismatchException(string message) :
            base(message)
        {
        }

        public PackageDownloadMismatchException(string expectedPackage, string actualPackage) :
            base(DefaultMessage(expectedPackage, actualPackage))
        {
        }

        private static string DefaultMessage(string expectedPackage, string actualPackage)
        {
            return $"{expectedPackage} is either not downloaded successfully due to wrong name, or downloaded package did not match expected package. Expected: {expectedPackage}, Actual: {actualPackage}";
        }
    }
}
