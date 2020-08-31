using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PortingAssistant.Model;

namespace PortingAssistant.NuGet
{
    public interface ICompatibilityChecker
    {
        public PackageSourceType GetCompatibilityCheckerType();
        public Dictionary<PackageVersionPair, Task<PackageDetails>> CheckAsync(List<PackageVersionPair> packageVersions, string pathToSolution);
    }
}
