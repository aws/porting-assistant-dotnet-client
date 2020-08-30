using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EncoreCommon.Model;

namespace EncoreCache
{
    public interface ICompatibilityChecker
    {
        public CompatibilityCheckerType GetCompatibilityCheckerType();
        public Dictionary<PackageVersionPair, Task<PackageDetails>> CheckAsync(List<PackageVersionPair> packageVersions, string pathToSolution);
    }
}
