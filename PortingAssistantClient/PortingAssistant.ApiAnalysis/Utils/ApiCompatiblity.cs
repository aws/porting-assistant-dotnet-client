using System;
using System.Linq;
using System.Threading.Tasks;
using PortingAssistant.Model;
using Semver;
using System.Collections.Generic;

namespace PortingAssistant.ApiAnalysis.Utils
{
    public static class ApiCompatiblity
    {
        public const string DEFAULT_TARGET = "netcoreapp3.1";

        public static CompatibilityResult GetCompatibilityResult(Task<PackageDetails> package, string apiMethodSignature, string version, string target = DEFAULT_TARGET, bool checkLesserPackage = true)
        {
            var compatiblityResult = new CompatibilityResult
            {
                Compatibility = Compatibility.UNKNOWN,
                CompatibleVersions = new List<string>()
            };

            if (package == null || apiMethodSignature == null ||!SemVersion.TryParse(version, out var targetversion))
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

            compatiblityResult.Compatibility = hasLesserTarget(version, framework.ToArray()) ? Compatibility.COMPATIBLE: Compatibility.INCOMPATIBLE;
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

            return targetVersions.Any(v => {
                if(!SemVersion.TryParse(v, out var semversion))
                    {
                        return false;
                    }
                return SemVersion.Compare(target, semversion) > 0;
            });
        }

        public static ApiRecommendation UpgradeStrategy(Task<PackageDetails> nugetPackage, string apiMethodSignature, string version, string nameSpaceToQuery, Dictionary<string, Task<RecommendationDetails>> _recommendationDetails)
        {
            if (nugetPackage == null || apiMethodSignature == null || version == null)
            {
                return new ApiRecommendation{
                    RecommendedActionType = RecommendedActionType.NoRecommendation
                };
            }

            nugetPackage.Wait();
            if (!nugetPackage.IsCompletedSuccessfully)
            {
                return new ApiRecommendation{
                    RecommendedActionType = RecommendedActionType.NoRecommendation
                };
            }
            var targetApi = GetApiDetails(nugetPackage.Result, apiMethodSignature);
            if (targetApi == null || targetApi.Targets == null || !targetApi.Targets.TryGetValue(DEFAULT_TARGET, out var versions))
            {
                return new ApiRecommendation{
                    RecommendedActionType = RecommendedActionType.NoRecommendation
                };
            }

            //var upgradeVersion = versions.Last();

            try
            {
                if (!hasLesserTarget(version, versions.ToArray()))
                {
                    // No Package upgrade. Check for API recommendation
                    if (_recommendationDetails.TryGetValue(nameSpaceToQuery, out var taskCompletionSource))
                        { 
                            var apiList = _recommendationDetails[nameSpaceToQuery];
                            apiList.Wait();
                            if (!apiList.IsCompletedSuccessfully)
                            {
                                return new ApiRecommendation{
                                    RecommendedActionType = RecommendedActionType.NoRecommendation
                                };
                            }

                            var recommendationActions = apiList.Result.RecommendedActions;
                            foreach (var eachRecommendationAPI in recommendationActions)
                            {
                                if (eachRecommendationAPI.Value == apiMethodSignature) 
                                {
                                    if (eachRecommendationAPI.Recommendation != null || eachRecommendationAPI.Recommendation.Length != 0){
                                        // First recommendation is the preferred one.
                                        return new ApiRecommendation{
                                            RecommendedActionType = RecommendedActionType.ReplaceApi,
                                            Description = eachRecommendationAPI.Recommendation.First().Description
                                        };
                                    }
                                }
                            }
                        }
                    else {
                            return new ApiRecommendation
                                {
                                    RecommendedActionType = RecommendedActionType.NoRecommendation
                                };
                        }
                }
                var upgradeVersion = versions.ToList().Find(v => {
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
            catch
            {
                return new ApiRecommendation
                    {
                        RecommendedActionType = RecommendedActionType.NoRecommendation
                    };
            }
        }

        private static ApiDetails GetApiDetails(PackageDetails nugetPackage, string apiMethodSignature)
        {
            if(nugetPackage == null || nugetPackage.Api == null || apiMethodSignature == null)
            {
                return null;
            }

            var foundApi = nugetPackage.Api.FirstOrDefault(api => api.MethodSignature == apiMethodSignature);
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
                    
                    try {
                        var possibleExtension = api.MethodParameters[0];
                        var methodSignatureIndex = api.MethodSignature.IndexOf("(") >= 0 ? api.MethodSignature.IndexOf("(") : api.MethodSignature.Length;
                        var sliceMethodSignature = api.MethodSignature.Substring(0, methodSignatureIndex);
                        var methondNameIndex = sliceMethodSignature.IndexOf(api.MethodName);
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
