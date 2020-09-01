using System;
namespace PortingAssistant.Model
{
    public class PackageVersionNotFoundException : Exception
    {
        public string PackageId { get; }
        public string Version { get; }
        public Exception Cause { get; }

        public PackageVersionNotFoundException(string packageId, string version, Exception innerException) :
            base("Package: " + packageId + ", version: " + version + " not found", innerException)
        {
            PackageId = packageId;
            Version = version;
            Cause = innerException;
        }
    }
}
