using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.NuGet
{
    public interface IPortingAssistantNuGetHandler
    {
        public Dictionary<PackageVersionPair, Task<PackageDetails>> GetNugetPackages(List<PackageVersionPair> nugetPackages, string pathToSolution);
        public Dictionary<PackageVersionPair, Task<PackageDetails>> GetAndCacheNugetPackages(List<PackageVersionPair> nugetPackages, string pathToSolution);
        public Dictionary<PackageVersionPair, Task<PackageDetails>> DownloadAndCacheNugetPackages(List<PackageVersionPair> nugetPackages, string pathToSolution);
    }
}
