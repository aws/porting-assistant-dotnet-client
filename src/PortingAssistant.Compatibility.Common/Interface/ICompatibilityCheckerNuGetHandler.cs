using Microsoft.Extensions.Logging;
using PortingAssistant.Compatibility.Common.Model;

namespace PortingAssistant.Compatibility.Common.Interface
{
    public interface ICompatibilityCheckerNuGetHandler
    {
        public Dictionary<PackageVersionPair, Task<PackageDetails>> GetNugetPackages(List<PackageVersionPair> allPackages);
    }
}
