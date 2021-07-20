﻿using System;
using System.Linq;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;
using System.Collections.Generic;
using NuGet.Versioning;

namespace PortingAssistant.Client.Analysis.Utils
{
    public static class ApiCompatiblity
    {
        private static readonly ApiRecommendation DEFAULT_RECOMMENDATION = new ApiRecommendation
        {
            RecommendedActionType = RecommendedActionType.NoRecommendation
        };

        public static CompatibilityResult GetCompatibilityResult(PackageDetailsWithApiIndices package, CodeEntityDetails codeEntityDetails, string target = "netcoreapp3.1", bool checkLesserPackage = false)
        {
            //If invocation, we will try to find it in a later package
            if (codeEntityDetails.CodeEntityType == CodeEntityType.Method)
            {
                return GetCompatibilityResult(package, codeEntityDetails.OriginalDefinition, codeEntityDetails.Package.Version, target, checkLesserPackage);
            }
            //If another node type, we will not try to find it. Compatibility will be based on the package compatiblity
            else
            {
                var compatiblityResult = new CompatibilityResult
                {
                    Compatibility = Compatibility.UNKNOWN,
                    CompatibleVersions = new List<string>()
                };

                if (package == null || !NuGetVersion.TryParse(codeEntityDetails.Package.Version, out var targetversion))
                {
                    return compatiblityResult;
                }

                if (package.PackageDetails.IsDeprecated)
                {
                    compatiblityResult.Compatibility = Compatibility.DEPRECATED;
                    return compatiblityResult;
                }

                //For other code entities, we just need to check if the package has a compatible target:
                if (package.PackageDetails.Targets.ContainsKey(target))
                {
                    compatiblityResult.Compatibility = Compatibility.COMPATIBLE;
                    return compatiblityResult;
                } else
                {
                    compatiblityResult.Compatibility = Compatibility.INCOMPATIBLE;
                }
                
                return compatiblityResult;
            }
        }


        public static CompatibilityResult GetCompatibilityResult(PackageDetailsWithApiIndices package, string apiMethodSignature, string version, string target = "netcoreapp3.1", bool checkLesserPackage = false)
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

            if (package.PackageDetails.IsDeprecated)
            {
                compatiblityResult.Compatibility = Compatibility.DEPRECATED;
                return compatiblityResult;
            }

            var foundApi = GetApiDetails(package, apiMethodSignature);

            if (foundApi == null)
            {
                if (!checkLesserPackage || package.PackageDetails.Targets == null || !package.PackageDetails.Targets.TryGetValue(target, out var targetFramework))
                {
                    compatiblityResult.Compatibility = Compatibility.INCOMPATIBLE;
                    return compatiblityResult;
                }

                compatiblityResult.Compatibility = HasLesserTarget(version, targetFramework.ToArray()) ? Compatibility.COMPATIBLE : Compatibility.INCOMPATIBLE;
                compatiblityResult.CompatibleVersions = targetFramework
                    .Where(v =>
                    {
                        if (!NuGetVersion.TryParse(v, out var semversion))
                        {
                            return false;
                        }
                        return semversion.CompareTo(targetversion) > 0;
                    }).ToList() ?? new List<string>();

                return compatiblityResult;
            }

            if (!foundApi.Targets.TryGetValue(target, out var framework))
            {
                compatiblityResult.Compatibility = Compatibility.INCOMPATIBLE;
                return compatiblityResult;
            }

            compatiblityResult.Compatibility = HasLesserTarget(version, framework.ToArray()) ? Compatibility.COMPATIBLE : Compatibility.INCOMPATIBLE;
            compatiblityResult.CompatibleVersions = framework
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

        private static bool HasLesserTarget(string version, string[] targetVersions)
        {
            if (!NuGetVersion.TryParse(version, out var target))
            {
                return false;
            }
            return targetVersions.Any(v =>
            {
                if (v == "0.0.0" || v == "0.0.0.0")
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
            Task<RecommendationDetails> recommendationDetails,
            string target = "netcoreapp3.1")
        {
            try
            {

                if (compatibilityResult?.CompatibleVersions != null)
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
                return FetchApiRecommendation(apiMethodSignature, recommendationDetails, target);
            }
            catch
            {
                return DEFAULT_RECOMMENDATION;
            }

        }

        private static ApiRecommendation FetchApiRecommendation(
            string apiMethodSignature,
            Task<RecommendationDetails> recommendationDetails,
            string target = "netcoreapp3.1")
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
                        .Where(recomendation => recomendation.TargetFrameworks.Contains(target.ToLower()))
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

        private static ApiDetails GetApiDetails(PackageDetailsWithApiIndices packageDetailsWithApiIndices, string apiMethodSignature)
        {
            if (packageDetailsWithApiIndices == null ||
                packageDetailsWithApiIndices.PackageDetails == null ||
                packageDetailsWithApiIndices.IndexDict == null ||
                packageDetailsWithApiIndices.PackageDetails.Api == null ||
                apiMethodSignature == null)
            {
                return null;
            }

            var index = packageDetailsWithApiIndices.IndexDict.GetValueOrDefault(apiMethodSignature.Replace("?", ""), -1);

            if (index >= 0 && index < packageDetailsWithApiIndices.PackageDetails.Api.Length)
            {
                return packageDetailsWithApiIndices.PackageDetails.Api[index];
            }

            return null;
        }

        public static Dictionary<PackageVersionPair, PackageDetailsWithApiIndices> PreProcessPackageDetails(Dictionary<PackageVersionPair, Task<PackageDetails>> packageResults)
        {
            return packageResults.Select(entity =>
            {
                try
                {
                    entity.Value.Wait();
                    if (entity.Value.IsCompletedSuccessfully)
                    {
                        var indexDict = SignatureToIndexPreProcess(entity.Value.Result);
                        return new Tuple<PackageVersionPair, PackageDetailsWithApiIndices>(entity.Key, new PackageDetailsWithApiIndices
                        {
                            PackageDetails = entity.Value.Result,
                            IndexDict = indexDict
                        });
                    }
                    return null;
                }
                catch
                {
                    return null;
                }
            }).Where(p => p != null).ToDictionary(t => t.Item1, t => t.Item2);
        }

        private static Dictionary<string, int> SignatureToIndexPreProcess(PackageDetails packageDetails)
        {
            var indexDict = new Dictionary<string, int>();
            if (packageDetails == null || packageDetails.Api == null)
            {
                return indexDict;
            }

            for (int i = 0; i < packageDetails.Api.Length; i++)
            {
                var api = packageDetails.Api[i];
                var signature = api.MethodSignature.Replace("?", "");
                if (!string.IsNullOrEmpty(signature) && !indexDict.ContainsKey(signature))
                {
                    indexDict.Add(signature, i);
                }

                var extensionSignature = GetExtensionSignature(api);
                if (!string.IsNullOrEmpty(extensionSignature) && !indexDict.ContainsKey(extensionSignature))
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
                if (api == null || api.MethodParameters == null || api.MethodParameters.Length == 0)
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
