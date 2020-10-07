using NuGet.Packaging.Core;

namespace PortingAssistant.Client.Model
{
    public static class ExceptionMessage
    {
        public static string PackageNotFound(PackageVersionPair packageVersion)
            => PackageNotFound(packageVersion.ToString());
        public static string PackageNotFound(string package)
            => $"Cannot find package {package}";
        public static string PackageSourceNotFound(PackageIdentity package)
            => PackageSourceNotFound(package.ToString());
        public static string PackageSourceNotFound(string packageVersion)
            => $"Package source not found for {packageVersion}";
        public static string NamespaceNotFound(string @namespace)
            => $"Could not find recommended namespace {@namespace}";
        public static string NamespaceFailedToProcess(string @namespace)
            => $"Failed to process namespace {@namespace}";
    }
}