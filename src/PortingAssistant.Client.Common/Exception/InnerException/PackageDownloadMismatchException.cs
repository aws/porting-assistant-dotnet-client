using System;

namespace PortingAssistant.Client.Model
{
    public class PackageDownloadMismatchException : Exception
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
            return $"Downloaded package did not match expected package. Expected: {expectedPackage}, Actual: {actualPackage}";
        }
    }
}
