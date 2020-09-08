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
        private const string DEFAULT_TARGET = "netcoreapp3.1";

        public static Compatibility apiInPackageVersion(Task<PackageDetails> package, string apiMethodSignature, string version, string target = DEFAULT_TARGET, bool checkLesserPackage = true)
        {

            if (package == null || apiMethodSignature == null)
            {
                return Compatibility.UNKNOWN;
            }

            package.Wait();
            if (!package.IsCompletedSuccessfully)
            {
                return Compatibility.UNKNOWN;
            }

            if (package.Result.Deprecated)
            {
                return Compatibility.DEPRACATED;
            }

            var foundApi = GetApiDetails(package.Result, apiMethodSignature);

            if (foundApi == null)
            {
                if (!checkLesserPackage || package.Result.Targets == null || !package.Result.Targets.TryGetValue(target, out var targetFramework))
                {
                    return Compatibility.INCOMPATIBLE;
                }

                return hasLesserTarget(version, targetFramework.ToArray()) ? Compatibility.COMPATIBLE : Compatibility.INCOMPATIBLE;
            }

            if (!foundApi.Targets.TryGetValue(target, out var framework))
            {
                return Compatibility.INCOMPATIBLE;
            }

            return hasLesserTarget(version, framework.ToArray()) ? Compatibility.COMPATIBLE: Compatibility.INCOMPATIBLE;

        }

        private static bool hasLesserTarget(string version, string[] targetVersions)
        {
            if (!SemVersion.TryParse(version, out var target))
            {
                return false;
            }

            return targetVersions.Any(v => SemVersion.Compare(target, SemVersion.Parse(v)) > 0);
        }

        public static ApiRecommendation upgradeStrategy(Task<PackageDetails> nugetPackage, string apiMethodSignature, string version, string nameSpaceToQuery, Dictionary<string, Task<RecommendationDetails>> _recommendationDetails)
        {
            if (nugetPackage == null || apiMethodSignature == null || version == null)
            {
                return new ApiRecommendation{
                    RecommendedActionType = RecommendedActionType.NoRecommendation,
                    desciption = null,
                    UpgradeVersion = null
                };
            }

            nugetPackage.Wait();
            if (!nugetPackage.IsCompletedSuccessfully)
            {
                return new ApiRecommendation{
                    RecommendedActionType = RecommendedActionType.NoRecommendation,
                    desciption = null,
                    UpgradeVersion = null
                };
            }
            var targetApi = GetApiDetails(nugetPackage.Result, apiMethodSignature);
            if (targetApi == null || targetApi.Targets == null || !targetApi.Targets.TryGetValue(DEFAULT_TARGET, out var versions))
            {
                return new ApiRecommendation{
                    RecommendedActionType = RecommendedActionType.NoRecommendation,
                    desciption = null,
                    UpgradeVersion = null
                };
            }

            var upgradeVersion = versions.Last();
            try
            {
                if (SemVersion.Compare(SemVersion.Parse(version), SemVersion.Parse(upgradeVersion)) > 0)
                {
                    // No Package upgrade. Check for API recommendation
                    if (_recommendationDetails.TryGetValue(nameSpaceToQuery, out var taskCompletionSource))
                        { 
                            var apiList = _recommendationDetails[nameSpaceToQuery];
                            apiList.Wait();
                            if (!apiList.IsCompletedSuccessfully)
                            {
                                return new ApiRecommendation{
                                    RecommendedActionType = RecommendedActionType.NoRecommendation,
                                    desciption = null,
                                    UpgradeVersion = null
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
                                            desciption = eachRecommendationAPI.Recommendation.First().Description,
                                            UpgradeVersion = null
                                        };
                                    }
                                }
                            }
                        }
                    else {
                            return new ApiRecommendation
                                {
                                    RecommendedActionType = RecommendedActionType.NoRecommendation,
                                    desciption = null,
                                    UpgradeVersion = null
                                };
                        }
                }
                return new ApiRecommendation
                    {
                        RecommendedActionType = RecommendedActionType.UpgradePackage,
                        desciption = null,
                        UpgradeVersion = upgradeVersion
                    };
 
            }
            catch
            {
                return new ApiRecommendation
                    {
                        RecommendedActionType = RecommendedActionType.NoRecommendation,
                        desciption = null,
                        UpgradeVersion = null
                    };
            }
        }

        private static ApiDetails GetApiDetails(PackageDetails nugetPackage, string apiMethodSignature)
        {
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

                    var possibleExtension = api.MethodParameters[0];
                    var sliceMethodSignature = api.MethodSignature.Substring(0, api.MethodSignature.IndexOf("("));
                    var methodName = sliceMethodSignature.Substring(sliceMethodSignature.LastIndexOf(api.MethodName));
                    var methodSignature = $"${possibleExtension}.${methodName}(${String.Join(",", api.MethodParameters.Take(1))}";
                    return methodSignature == apiMethodSignature.Replace("?", "");
                });
            }

            return foundApi;
        }
    }
}
