using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PortingAssistant.Model;

namespace PortingAssistant.NuGet
{
    /// <summary>
    /// Represents a compatibility checker for packages
    /// </summary>
    public interface ICompatibilityChecker
    {
        /// <summary>
        /// Gets the type of the compatiblity checker
        /// </summary>
        /// <returns>The type of the compatibility checker</returns>
        public PackageSourceType GetCompatibilityCheckerType();

        /// <summary>
        /// Runs the compatibility check
        /// </summary>
        /// <param name="packageVersions">A List of packages and their versions</param>
        /// <param name="pathToSolution">The solution to check</param>
        /// <returns></returns>
        public Dictionary<PackageVersionPair, Task<PackageDetails>> CheckAsync(List<PackageVersionPair> packageVersions, string pathToSolution);
    }
}
