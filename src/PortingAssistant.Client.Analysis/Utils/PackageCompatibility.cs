using System;
using System.Collections.Generic;
using System.Linq;
using PortingAssistant.Client.Model;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace PortingAssistant.Client.Analysis.Utils
{
    public static class PackageCompatibility
    {
        public static async Task<PackageAnalysisResult> GetPackageAnalysisResult(Task<CompatibilityResult> CompatibilityResult, PackageVersionPair packageVersionPair, string targetFramework)
        {
            var result = await CompatibilityResult;
            return new PackageAnalysisResult
            {
                PackageVersionPair = packageVersionPair,
                CompatibilityResults = new Dictionary<string, CompatibilityResult>
                {
                    {
                        targetFramework, new CompatibilityResult
                        {
                            Compatibility = result.Compatibility,
                            CompatibleVersions = result.CompatibleVersions
                        }
                    }
                },
                Recommendations = new PortingAssistant.Client.Model.Recommendations
                {
                    RecommendedActions = new List<PortingAssistant.Client.Model.RecommendedAction>
                    {
                        new PackageRecommendation
                        {
                            PackageId = packageVersionPair.PackageId,
                            RecommendedActionType = RecommendedActionType.UpgradePackage,
                            Description = result.CompatibleVersions.Count != 0 ? result.CompatibleVersions.First() : null
                        }
                    }
                }
            };
        }

        public static async Task<CompatibilityResult> IsCompatibleAsync(Task<PackageDetails> packageDetails, PackageVersionPair packageVersionPair, ILogger _logger, string target = "netcoreapp3.1")
        {
            if (packageDetails == null || packageVersionPair == null)
            {
                return new CompatibilityResult
                {
                    Compatibility = Compatibility.UNKNOWN,
                    CompatibleVersions = new List<string>()
                };
            }

            try
            {
                await packageDetails;
                if (!packageDetails.IsCompletedSuccessfully)
                {
                    return new CompatibilityResult
                    {
                        Compatibility = Compatibility.UNKNOWN,
                        CompatibleVersions = new List<string>()
                    };
                }

                var foundTarget = packageDetails.Result.Targets.GetValueOrDefault(target, null);
                if (foundTarget == null)
                {
                    return new CompatibilityResult
                    {
                        Compatibility = Compatibility.INCOMPATIBLE,
                        CompatibleVersions = new List<string>()
                    };
                }

                if (!NuGetVersion.TryParse(packageVersionPair.Version, out var version))
                {
                    return new CompatibilityResult
                    {
                        Compatibility = Compatibility.UNKNOWN,
                        CompatibleVersions = new List<string>()
                    };
                }

                var compatibleVersions = foundTarget.Where(v =>
                {
                    if (!NuGetVersion.TryParse(v, out var semversion))
                    {
                        return false;
                    }
                    return semversion.CompareTo(version) > 0;
                }).ToList();

                compatibleVersions.Sort( (a, b) => NuGetVersion.Parse(a).CompareTo(NuGetVersion.Parse(b)));
                return new CompatibilityResult
                {
                    Compatibility = foundTarget.Any(v =>
                    {
                        if (!NuGetVersion.TryParse(v, out var semversion))
                        {
                            return false;
                        }

                        return version.CompareTo(semversion) >= 0;
                    }) ? Compatibility.COMPATIBLE : Compatibility.INCOMPATIBLE,
                    CompatibleVersions = compatibleVersions
                };
            }
            catch (Exception e)
            {
                _logger.LogError("parse package version {0} {1} with error {2}", packageVersionPair.PackageId, packageVersionPair.Version, e);
                return new CompatibilityResult
                {
                    Compatibility = Compatibility.UNKNOWN,
                    CompatibleVersions = new List<string>()
                };
            }

        }
    }
}
