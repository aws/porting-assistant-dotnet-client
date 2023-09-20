
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using PortingAssistant.Compatibility.Common.Model;

namespace PortingAssistant.Compatibility.Common.Interface
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
        /// <returns></returns>
        //public Task<Dictionary<PackageVersionPair, Task<PackageDetails>>> Check(
        //    IEnumerable<PackageVersionPair> packageVersions);
        public Dictionary<PackageVersionPair, Task<PackageDetails>> Check(
            IEnumerable<PackageVersionPair> packageVersions);
    }
}
