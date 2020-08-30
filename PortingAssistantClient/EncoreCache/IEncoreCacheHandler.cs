using System.Collections.Generic;
using System.Threading.Tasks;
using EncoreCommon.Model;

namespace EncoreCache
{
    public interface IEncoreCacheHandler
    {
        public Dictionary<PackageVersionPair, Task<PackageVersionResult>> GetNugetPackages(List<PackageVersionPair> nugetPackages, string pathToSolution);
        public Task<PackageDetails> GetPackageDetails(PackageVersionPair package);
    }
}
