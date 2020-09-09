using System;
using System.Collections.Generic;
using System.Linq;
using PortingAssistant.Model;
using Semver;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace PortingAssistant.Utils
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
                CompatibilityResult = new Dictionary<string, CompatibilityResult>
                    {
                        {
                            DEFAULT_TARGET, new CompatibilityResult
                            {
                                Compatibility = result.Compatibility,
                                CompatibleVersion = result.CompatibleVersion
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
                            TargetVersions = result.CompatibleVersion
                        }
                    }
                }
            };
        }

        public static async Task<CompatibilityResult> isCompatibleAsync(Task<PackageDetails> packageDetails, PackageVersionPair packageVersionPair, ILogger _logger, string target = DEFAULT_TARGET)
        {
            try
            {
                await packageDetails;
                if (!packageDetails.IsCompletedSuccessfully)
                {
                    return new CompatibilityResult
                    {
                        Compatibility = Compatibility.UNKNOWN,
                        CompatibleVersion = new List<string>()
                    };
                }

                var foundTarget = packageDetails.Result.Targets.GetValueOrDefault(target, null);
                if (foundTarget == null)
                {
                    return new CompatibilityResult
                    {
                        Compatibility = Compatibility.INCOMPATIBLE,
                        CompatibleVersion = new List<string>()
                    };
                }
                if (!SemVersion.TryParse(packageVersionPair.Version, out var version))
                {
                    return new CompatibilityResult
                    {
                        Compatibility = Compatibility.UNKNOWN,
                        CompatibleVersion = new List<string>()
                    };
                }
                return new CompatibilityResult
                {
                    Compatibility = foundTarget.Any(v => SemVersion.Compare(version, SemVersion.Parse(v)) >= 0) ? Compatibility.COMPATIBLE : Compatibility.INCOMPATIBLE,
                    CompatibleVersion = foundTarget.Where(v => SemVersion.Compare(SemVersion.Parse(v), version) > 0).ToList()
                };
            }
            catch (Exception e)
            {
                _logger.LogError("parse package version {0} {1}with error {2}", packageVersionPair.PackageId, packageVersionPair.Version, e);
                return new CompatibilityResult
                {
                    Compatibility = Compatibility.UNKNOWN,
                    CompatibleVersion = new List<string>()
                };
            }

        }
    }
}
