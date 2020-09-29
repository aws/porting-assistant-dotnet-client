using System;
using System.Linq;
using System.Threading.Tasks;
using PortingAssistant.Model;
using Semver;
using System.Collections.Generic;

namespace PortingAssistant.Analysis.Utils
{
    public static class ApiCompatiblity
    {
        public const string DEFAULT_TARGET = "netcoreapp3.1";
        private static readonly ApiRecommendation DEFAULT_RECOMMENDATION = new ApiRecommendation
        {
            RecommendedActionType = RecommendedActionType.NoRecommendation
        };

        public static CompatibilityResult GetCompatibilityResult(Task<PackageDetails> package, string apiMethodSignature, string version, string target = DEFAULT_TARGET, bool checkLesserPackage = true)
        {
            var compatiblityResult = new CompatibilityResult
            {
                Compatibility = Compatibility.UNKNOWN,
                CompatibleVersions = new List<string>()
            };

            if (package == null || apiMethodSignature == null || !SemVersion.TryParse(version, out var targetversion))
            {
                return compatiblityResult;
            }

            package.Wait();
            if (!package.IsCompletedSuccessfully)
            {
                return compatiblityResult;
            }

            if (package.Result.IsDeprecated)
            {
                compatiblityResult.Compatibility = Compatibility.DEPRECATED;
                return compatiblityResult;
            }

            var foundApi = GetApiDetails(package.Result, apiMethodSignature);

            if (foundApi == null)
            {
                if (!checkLesserPackage || package.Result.Targets == null || !package.Result.Targets.TryGetValue(target, out var targetFramework))
                {
                    compatiblityResult.Compatibility = Compatibility.INCOMPATIBLE;
                    return compatiblityResult;
                }

                compatiblityResult.Compatibility = hasLesserTarget(version, targetFramework.ToArray()) ? Compatibility.COMPATIBLE : Compatibility.INCOMPATIBLE;
                compatiblityResult.CompatibleVersions = targetFramework.ToArray()
                    .Where(v =>
                    {
                        if (!SemVersion.TryParse(v, out var semversion))
                        {
                            return false;
                        }
                        return SemVersion.Compare(semversion, targetversion) > 0;
                    }).ToList();
                return compatiblityResult;
            }

            if (!foundApi.Targets.TryGetValue(target, out var framework))
            {
                compatiblityResult.Compatibility = Compatibility.INCOMPATIBLE;
                return compatiblityResult;
            }

            compatiblityResult.Compatibility = hasLesserTarget(version, framework.ToArray()) ? Compatibility.COMPATIBLE : Compatibility.INCOMPATIBLE;
            compatiblityResult.CompatibleVersions = framework.ToArray()
                .Where(v =>
                {
                    if (!SemVersion.TryParse(v, out var semversion))
                    {
                        return false;
                    }
                    return SemVersion.Compare(semversion, targetversion) > 0;
                }).ToList();
            return compatiblityResult;
        }

        private static bool hasLesserTarget(string version, string[] targetVersions)
        {
            if (!SemVersion.TryParse(version, out var target))
            {
                return false;
            }

            return targetVersions.Any(v =>
            {
                if (!SemVersion.TryParse(v, out var semversion))
                {
                    return false;
                }
                return SemVersion.Compare(target, semversion) > 0;
            });
        }

        public static ApiRecommendation UpgradeStrategy(
            Task<PackageDetails> nugetPackage,
            string apiMethodSignature,
            string version,
            string nameSpaceToQuery,
            Dictionary<string, Task<RecommendationDetails>> _recommendationDetails)
        {
            try
            {
                if (nugetPackage != null && apiMethodSignature != null && version == null)
                {
                    var UpgradeVersionRecommendation = UpgradePackageVersion(nugetPackage, apiMethodSignature, version);
                    if (UpgradeVersionRecommendation != null && UpgradeVersionRecommendation.RecommendedActionType == RecommendedActionType.UpgradePackage)
                        return UpgradeVersionRecommendation;
                }
                return FetchApiRecommendation(apiMethodSignature, nameSpaceToQuery, _recommendationDetails);
            }
            catch
            {
                return DEFAULT_RECOMMENDATION;
            }
    
        }
        private static ApiRecommendation UpgradePackageVersion(
                       Task<PackageDetails> nugetPackage,
                       string apiMethodSignature,
                       string version)
        {
            nugetPackage.Wait();
            if (nugetPackage.IsCompletedSuccessfully)
            {
                var targetApi = GetApiDetails(nugetPackage.Result, apiMethodSignature);
                if (targetApi != null && targetApi.Targets != null && targetApi.Targets.TryGetValue(DEFAULT_TARGET, out var versions))
                {
                    if (hasLesserTarget(version, versions.ToArray()))
                    {
                        var upgradeVersion = versions.ToList().Find(v =>
                        {
                            if (!SemVersion.TryParse(v, out var semversion) || !SemVersion.TryParse(version, out var target))
                            {
                                return false;
                            }
                            return SemVersion.Compare(semversion, target) > 0;
                        });
                        return new ApiRecommendation
                        {
                            RecommendedActionType = RecommendedActionType.UpgradePackage,
                            Description = upgradeVersion
                        };
                    }
                }
            }
            return DEFAULT_RECOMMENDATION;
        }

        private static ApiRecommendation FetchApiRecommendation(
            string apiMethodSignature,
            string nameSpaceToQuery,
            Dictionary<string, Task<RecommendationDetails>> _recommendationDetails)
        {
            if (apiMethodSignature != null && _recommendationDetails.TryGetValue(nameSpaceToQuery, out var namespacesRecommendation))
            {
                namespacesRecommendation.Wait();
                if (namespacesRecommendation.IsCompletedSuccessfully)
                {
                    var recommendationActions = namespacesRecommendation.Result.Recommendedations;
                    var apiRecommendation = recommendationActions
                        .Where(recommendation => recommendation != null && recommendation.Value == apiMethodSignature)
                        .SelectMany(recommendation => recommendation.Recommendation)
                        .Select(recommend => recommend.Description);
                    if (apiRecommendation != null && apiRecommendation.Count() != 0)
                    {
                        return new ApiRecommendation
                        {
                            RecommendedActionType = RecommendedActionType.ReplaceApi,
                            Description = string.Join(",",apiRecommendation.Where(recommend=> !string.IsNullOrEmpty(recommend)))
                        };
                    }
                }
            }
            return DEFAULT_RECOMMENDATION;
        }

            private static ApiDetails GetApiDetails(PackageDetails nugetPackage, string apiMethodSignature)
        {
            if (nugetPackage == null || nugetPackage.Api == null || apiMethodSignature == null)
            {
                return null;
            }

            var foundApi = nugetPackage.Api.FirstOrDefault(api => api.MethodSignature == apiMethodSignature.Replace("?", ""));
            if (foundApi == null)
            {
                foundApi = nugetPackage.Api.FirstOrDefault(api =>
                {
                    if (
                    api.MethodParameters == null ||
                    api.MethodParameters.Length == 0 ||
                    api.MethodSignature == null ||
                    api.MethodName == null
                    )
                    {
                        return false;
                    }

                    try
                    {
                        var possibleExtension = api.MethodParameters[0];
                        var methodSignatureIndex = api.MethodSignature.IndexOf("(") >= 0 ? api.MethodSignature.IndexOf("(") : api.MethodSignature.Length;
                        var sliceMethodSignature = api.MethodSignature.Substring(0, methodSignatureIndex);
                        var methondNameIndex = sliceMethodSignature.LastIndexOf(api.MethodName);
                        var methodName = sliceMethodSignature.Substring(methondNameIndex >= 0 ? methondNameIndex : sliceMethodSignature.Length);
                        var methodSignature = $"{possibleExtension}.{methodName}({String.Join(",", api.MethodParameters.Skip(1))})";
                        return methodSignature == apiMethodSignature.Replace("?", "");
                    }
                    catch
                    {
                        return false;
                    }
                });
            }

            return foundApi;
        }
    }
}
