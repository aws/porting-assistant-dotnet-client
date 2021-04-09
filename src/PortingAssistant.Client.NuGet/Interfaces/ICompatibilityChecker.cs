using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.NuGet
{
    /// <summary>
    /// Represents a compatibility checker for packages
    /// </summary>
    public interface ICompatibilityChecker
    {
        /// <summary>
        /// Gets the type of the compatibility checker
        /// </summary>
        /// <returns>The type of the compatibility checker</returns>
        public PackageSourceType CompatibilityCheckerType { get; }

        /// <summary>
        /// Runs the compatibility check
        /// </summary>
        /// <param name="packageVersions">A collection of packages and their versions</param>
        /// <param name="pathToSolution">The solution to check</param>
        /// <param name="isIncremental">If Check is part of incremental assessment, we will use Temp Directory Cache. Default to false</param>
        /// <param name="incrementalRefresh">If Check should refresh Temp Directory Cache. Default to false.</param>
        /// <returns></returns>
        public Dictionary<PackageVersionPair, Task<PackageDetails>> Check(IEnumerable<PackageVersionPair> packageVersions, string pathToSolution, bool isIncremental = false, bool incrementalRefresh = false);
    }
}
