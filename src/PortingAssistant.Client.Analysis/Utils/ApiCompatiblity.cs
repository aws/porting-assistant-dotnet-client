using System;
using System.Linq;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;
using Semver;
using System.Collections.Generic;
using NuGet.Versioning;

namespace PortingAssistant.Client.Analysis.Utils
{
    public static class ApiCompatiblity
    {
        public const string DEFAULT_TARGET = "netcoreapp3.1";
        private static Dictionary<PackageDetails, Dictionary<string, int>> preIndexDict = new Dictionary<PackageDetails, Dictionary<string, int>>();
        private static readonly ApiRecommendation DEFAULT_RECOMMENDATION = new ApiRecommendation
        {
            RecommendedActionType = RecommendedActionType.NoRecommendation
        };

        public static CompatibilityResult GetCompatibilityResult(Task<PackageDetails> package, string apiMethodSignature, string version, string target = DEFAULT_TARGET, bool checkLesserPackage = false)
        {

            var compatiblityResult = new CompatibilityResult
            {
                Compatibility = Compatibility.UNKNOWN,
                CompatibleVersions = new List<string>()
            };

            if (package == null || apiMethodSignature == null || !NuGetVersion.TryParse(version, out var targetversion))
            {
                return compatiblityResult;
            }

            try
            {
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
                            if (!NuGetVersion.TryParse(v, out var semversion))
                            {
                                return false;
                            }
                            return semversion.CompareTo(targetversion) > 0;
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
                        if (!NuGetVersion.TryParse(v, out var semversion))
                        {
                            return false;
                        }
                        return semversion.CompareTo(targetversion) > 0;
                    }).ToList();
                return compatiblityResult;
            }
            catch
            {
                return compatiblityResult;
            }
        }

        private static bool hasLesserTarget(string version, string[] targetVersions)
        {
            if (!NuGetVersion.TryParse(version, out var target))
            {
                return false;
            }
            return targetVersions.Any(v =>
            {
                if (v == "0.0.0.0")
                {
                    return true;
                }
                if (!NuGetVersion.TryParse(v, out var semversion))
                {
                    return false;
                }
                return target.CompareTo(semversion) >= 0;
            });
        }

        public static ApiRecommendation UpgradeStrategy(
            CompatibilityResult compatibilityResult,
            string apiMethodSignature,
            string nameSpaceToQuery,
            Task<RecommendationDetails> recommendationDetails)
        {
            try
            {

                if (compatibilityResult != null && compatibilityResult.CompatibleVersions != null)
                {
                    var validVersions = compatibilityResult.CompatibleVersions.Where(v => !v.Contains("-")).ToList();
                    if (validVersions.Count != 0)
                    {
                        return new ApiRecommendation
                        {
                            RecommendedActionType = RecommendedActionType.UpgradePackage,
                            Description = validVersions.FirstOrDefault()
                        };
                    }
                }
                return FetchApiRecommendation(apiMethodSignature, nameSpaceToQuery, recommendationDetails);
            }
            catch
            {
                return DEFAULT_RECOMMENDATION;
            }

        }

        private static ApiRecommendation FetchApiRecommendation(
            string apiMethodSignature,
            string nameSpaceToQuery,
            Task<RecommendationDetails> recommendationDetails)
        {
            if (apiMethodSignature != null && recommendationDetails != null)
            {
                recommendationDetails.Wait();
                if (recommendationDetails.IsCompletedSuccessfully)
                {
                    var recommendationActions = recommendationDetails.Result.Recommendations;
                    var apiRecommendation = recommendationActions
                        .Where(recommendation => recommendation != null && recommendation.Value == apiMethodSignature)
                        .SelectMany(recommendation => recommendation.RecommendedActions)
                        .Select(recommend => recommend.Description);
                    if (apiRecommendation != null && apiRecommendation.Count() != 0)
                    {
                        return new ApiRecommendation
                        {
                            RecommendedActionType = RecommendedActionType.ReplaceApi,
                            Description = string.Join(",", apiRecommendation.Where(recommend => !string.IsNullOrEmpty(recommend)))
                        };
                    }
                }
            }
            return DEFAULT_RECOMMENDATION;
        }

        private static ApiDetails GetApiDetails(PackageDetails packageDetails, string apiMethodSignature)
        {
            if (packageDetails == null || packageDetails.Api == null || apiMethodSignature == null)
            {
                return null;
            }

            if (!preIndexDict.ContainsKey(packageDetails))
            {
                var indexDict = signatureToIndexPreProcess(packageDetails);
                preIndexDict.Add(packageDetails, indexDict);
            }

            if (!preIndexDict.TryGetValue(packageDetails, out var signatureToIndex))
            {
                return null;
            }

            var index = signatureToIndex.GetValueOrDefault(apiMethodSignature.Replace("?", ""), -1);

            if (index >= 0 && index < packageDetails.Api.Count())
            {
                return packageDetails.Api[index];
            }

            return null;
        }

        private static Dictionary<string, int> signatureToIndexPreProcess(PackageDetails packageDetails)
        {
            var indexDict = new Dictionary<string, int>();
            if (packageDetails == null || packageDetails.Api == null)
            {
                return indexDict;
            }

            for (int i = 0; i < packageDetails.Api.Count(); i++)
            {
                var api = packageDetails.Api[i];
                var signature = api.MethodSignature.Replace("?", "");
                if (signature != null && signature != "" && !indexDict.ContainsKey(signature))
                {
                    indexDict.Add(signature, i);
                }

                var extensionSignature = GetExtensionSignature(api);
                if (extensionSignature != null && extensionSignature != "" && !indexDict.ContainsKey(extensionSignature))
                {
                    indexDict.Add(extensionSignature, i);
                }

            }

            return indexDict;
        }

        private static string GetExtensionSignature(ApiDetails api)
        {
            try
            {
                if (api == null || api.MethodParameters == null || api.MethodParameters.Count() == 0)
                {
                    return null;
                }

                var possibleExtension = api.MethodParameters[0];
                var methodSignatureIndex = api.MethodSignature.IndexOf("(") >= 0 ? api.MethodSignature.IndexOf("(") : api.MethodSignature.Length;
                var sliceMethodSignature = api.MethodSignature.Substring(0, methodSignatureIndex);
                var methondNameIndex = sliceMethodSignature.LastIndexOf(api.MethodName);
                var methodName = sliceMethodSignature.Substring(methondNameIndex >= 0 ? methondNameIndex : sliceMethodSignature.Length);
                var methodSignature = $"{possibleExtension}.{methodName}({String.Join(", ", api.MethodParameters.Skip(1))})";
                return methodSignature;
            }
            catch
            {
                return null;
            }
        }
    }
}
