using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using PortingAssistant.Compatibility.Common.Model;


namespace PortingAssistant.Compatibility.Common.Utils
{
    public static class PackageCompatibility
    {
        public static PackageAnalysisResult GetPackageAnalysisResult(CompatibilityResult result, PackageVersionPair packageVersionPair,
            string targetFramework, AssessmentType assessmentType)
        {
            var compatibleVersions = result.GetCompatibleVersionsWithoutPreReleases();
            
            return new PackageAnalysisResult()
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
                Recommendations = new Recommendations
                {
                    RecommendedActions = new List<Recommendation>
                    {
                        new Recommendation
                        {
                            PackageId = packageVersionPair.PackageId,
                            RecommendedActionType = RecommendedActionType.UpgradePackage,
                            Description = compatibleVersions.Count != 0 ? compatibleVersions.First() : null,
                            TargetVersions = compatibleVersions
                        }
                    },
                    RecommendedPackageVersions = compatibleVersions
                }
            };
            
        }
        
        private static Recommendation FetchPackageRecommendation(
            PackageVersionPair packageVersionPair, List<string> compatibleVersions)
        {
            if (compatibleVersions.Count != 0)
            {
                return new Recommendation()
                {
                    PackageId = packageVersionPair.PackageId,
                    RecommendedActionType = RecommendedActionType.UpgradePackage,
                    Description = compatibleVersions.Count != 0 ? compatibleVersions.First() : null,
                    TargetVersions = compatibleVersions,
                    Version = packageVersionPair.Version
                };
            }

            return new Recommendation()
            {
                PackageId = packageVersionPair.PackageId,
                RecommendedActionType = RecommendedActionType.NoRecommendation,
                Description = compatibleVersions.Count != 0 ? compatibleVersions.First() : null,
                TargetVersions = compatibleVersions,
                Version = packageVersionPair.Version
            };
        }
        
        public static async Task<CompatibilityResult> IsCompatibleAsync(Task<PackageDetails> packageDetails,
            PackageVersionPair packageVersionPair, ILogger logger, string target = Constants.DefaultAssessmentTargetFramework)
        {
            if (packageDetails == null || packageVersionPair == null)
            {
                return new CompatibilityResult
                {
                    Compatibility = Model.Compatibility.UNKNOWN,
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
                        Compatibility = Model.Compatibility.UNKNOWN,
                        CompatibleVersions = new List<string>()
                    };
                }

                var compatibleVersionsForTargetFramework =
                    packageDetails.Result.Targets.GetValueOrDefault(target, null);
                if (compatibleVersionsForTargetFramework == null)
                {
                    return new CompatibilityResult
                    {
                        Compatibility = Model.Compatibility.INCOMPATIBLE,
                        CompatibleVersions = new List<string>()
                    };
                }

                if (!NuGetVersion.TryParse(packageVersionPair.Version, out var version))
                {
                    return new CompatibilityResult
                    {
                        Compatibility = Model.Compatibility.UNKNOWN,
                        CompatibleVersions = new List<string>()
                    };
                }

                var compatibleVersionsToRecommend =
                    version.FindGreaterCompatibleVersions(compatibleVersionsForTargetFramework).ToList();
                compatibleVersionsToRecommend.Sort((a, b) => NuGetVersion.Parse(a).CompareTo(NuGetVersion.Parse(b)));
                Model.Compatibility compatibility;
                
                var maxCompatibleVersion = NugetVersionHelper.GetMaxVersion(compatibleVersionsForTargetFramework);
                if (maxCompatibleVersion != null
                    && !maxCompatibleVersion.IsZeroVersion()
                    && version.IsGreaterThan(maxCompatibleVersion))
                {
                    compatibility = version.HasSameMajorAs(maxCompatibleVersion)
                        ? Model.Compatibility.COMPATIBLE
                        : Model.Compatibility.INCOMPATIBLE;
                }
                else
                {
                    compatibility = version.HasLowerOrEqualCompatibleVersion(compatibleVersionsForTargetFramework)
                        ? Model.Compatibility.COMPATIBLE
                        : Model.Compatibility.INCOMPATIBLE;
                }

                return new CompatibilityResult
                {
                    Compatibility = compatibility,
                    CompatibleVersions = compatibleVersionsToRecommend
                };
            }
            catch (OutOfMemoryException e)
            {
                logger.LogError($"parse package version {packageVersionPair.PackageId} {packageVersionPair.Version} with error {e}");
                return new CompatibilityResult
                {
                    Compatibility = Model.Compatibility.OUT_OF_MEMORY_PARSE_ERROR,
                    CompatibleVersions = new List<string>()
                };
            }
            catch (Exception e)
            {
                logger.LogError($"parse package version {packageVersionPair.PackageId} {packageVersionPair.Version} with error {e}");
                return new CompatibilityResult
                {
                    Compatibility = Model.Compatibility.GENERAL_PARSE_ERROR,
                    CompatibleVersions = new List<string>()
                };
            }

        }
    }
}
