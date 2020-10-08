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
        public const string DEFAULT_TARGET = "netcoreapp3.1";

        public static async Task<PackageAnalysisResult> GetPackageAnalysisResult(Task<CompatibilityResult> CompatibilityResult, PackageVersionPair packageVersionPair)
        {
            var result = await CompatibilityResult;
            return new PackageAnalysisResult
            {
                PackageVersionPair = packageVersionPair,
                CompatibilityResults = new Dictionary<string, CompatibilityResult>
                {
                    {
                        DEFAULT_TARGET, new CompatibilityResult
                        {
                            Compatibility = result.Compatibility,
                            CompatibleVersions = result.CompatibleVersions
                        }
                    }
                },
                Recommendations = new Recommendations
                {
                    RecommendedActions = new List<RecommendedAction>
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

        public static async Task<CompatibilityResult> isCompatibleAsync(Task<PackageDetails> packageDetails, PackageVersionPair packageVersionPair, ILogger _logger, string target = DEFAULT_TARGET)
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
                    CompatibleVersions = foundTarget.Where(v =>
                    {
                        if (!NuGetVersion.TryParse(v, out var semversion))
                        {
                            return false;
                        }
                        return semversion.CompareTo(version) > 0;
                    }).ToList()
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
