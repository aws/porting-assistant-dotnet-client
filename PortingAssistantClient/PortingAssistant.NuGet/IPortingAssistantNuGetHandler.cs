using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Model;

namespace PortingAssistant.NuGet
{
    public interface IPortingAssistantNuGetHandler
    {
        public Dictionary<PackageVersionPair, Task<PackageAnalysisResult>> GetNugetPackages(List<PackageVersionPair> nugetPackages, string pathToSolution);
        public Task<PackageDetails> GetPackageDetails(PackageVersionPair package);
    }
}
