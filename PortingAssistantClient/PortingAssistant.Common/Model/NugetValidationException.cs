using System;
namespace PortingAssistant.Model
{
    public class NugetValidationException : Exception
    {
        public PackageDetails Package;

        public NugetValidationException(PackageDetails package) :
            base("Package: " + package)
        {
            Package = package;
        }
    }
}
