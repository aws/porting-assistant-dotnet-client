using System;

namespace PortingAssistant.Client.Model
{
    public class PackageNotFoundException : Exception
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
