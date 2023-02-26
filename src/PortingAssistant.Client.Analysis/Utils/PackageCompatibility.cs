using System;
using System.Collections.Generic;
using System.Linq;
using PortingAssistant.Client.Common.Utils;
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
            var compatibleVersions = result.GetCompatibleVersionsWithoutPreReleases();
            return new PackageAnalysisResult
            {
                PackageVersionPair = packageVersionPair,
                CompatibilityResults = new Dictionary<string, CompatibilityResult>
                {
                    {
                        targetFramework, new CompatibilityResult
                        {
                            Compatibility = result.Compatibility,
                            CompatibleVersions = compatibleVersions
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
                            Description = compatibleVersions.Count != 0 ? compatibleVersions.First() : null,
                            TargetVersions = compatibleVersions
                        }
                    }
                }
            };
        }

        public static async Task<CompatibilityResult> IsCompatibleAsync(Task<PackageDetails> packageDetails, PackageVersionPair packageVersionPair, ILogger _logger, string target = "net6.0")
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

                var compatibleVersionsForTargetFramework = packageDetails.Result.Targets.GetValueOrDefault(target, null);
                if (compatibleVersionsForTargetFramework == null)
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
                
                Compatibility compatibility;
                var maxCompatibleVersion = NugetVersionHelper.GetMaxVersion(compatibleVersionsForTargetFramework);
                var maxVersionInDatastore =
                    NugetVersionHelper.GetMaxVersion(packageDetails.Result.Versions.ToNugetVersionCollection());
                var compatibleVersionsToRecommend = version.FindGreaterCompatibleVersions(compatibleVersionsForTargetFramework).ToList();
                compatibleVersionsToRecommend.Sort((a, b) => NuGetVersion.Parse(a).CompareTo(NuGetVersion.Parse(b)));

                if (compatibleVersionsForTargetFramework.Contains(version.OriginalVersion))
                {
                    compatibility = Compatibility.COMPATIBLE;
                }
                else if (version.IsGreaterThan(maxVersionInDatastore))
                {
                    compatibility = Compatibility.UNKNOWN;
                }
                //else if (maxCompatibleVersion != null
                //    && !maxCompatibleVersion.IsZeroVersion()
                //    && version.IsGreaterThan(maxCompatibleVersion))
                //{
                //    compatibility = version.HasSameMajorAs(maxCompatibleVersion)
                //        ? Compatibility.COMPATIBLE
                //        : Compatibility.INCOMPATIBLE;
                //}
                else
                {
                    compatibility = Compatibility.INCOMPATIBLE;
                }

                return new CompatibilityResult
                {
                    Compatibility = compatibility,
                    CompatibleVersions = compatibleVersionsToRecommend
                };
            }
            catch (OutOfMemoryException e)
            {
                _logger.LogError("parse package version {0} {1} with error {2}", packageVersionPair.PackageId, packageVersionPair.Version, e);
                MemoryUtils.LogMemoryConsumption(_logger);
                return new CompatibilityResult
                {
                    Compatibility = Compatibility.UNKNOWN,
                    CompatibleVersions = new List<string>()
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
