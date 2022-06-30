using System;
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

        public static CompatibilityResult GetCompatibilityResult(PackageDetailsWithApiIndices package, CodeEntityDetails codeEntityDetails, string target = "net6.0", bool checkLesserPackage = false)
        {
            //If invocation, we will try to find it in a later package
            if (codeEntityDetails.CodeEntityType == CodeEntityType.Method)
            {
                if (codeEntityDetails.Namespace == "")
                {
                    // codelyzer was not able to parse the symbol from the semantic model, so we can't accurately assess the compatibility.
                    return new CompatibilityResult
                    {
                        Compatibility = Compatibility.UNKNOWN,
                        CompatibleVersions = new List<string>()
                    };
                }
                return GetCompatibilityResult(package, codeEntityDetails.OriginalDefinition, codeEntityDetails.Package.Version, target, checkLesserPackage);
            }
            //If another node type, we will not try to find it. Compatibility will be based on the package compatibility
            else
            {
                var compatibilityResult = new CompatibilityResult
                {
                    Compatibility = Compatibility.UNKNOWN,
                    CompatibleVersions = new List<string>()
                };

                if (package == null || !NuGetVersion.TryParse(codeEntityDetails.Package.Version, out var targetversion))
                {
                    return compatibilityResult;
                }

                if (package.PackageDetails.IsDeprecated)
                {
                    compatibilityResult.Compatibility = Compatibility.DEPRECATED;
                    return compatibilityResult;
                }

                //For other code entities, we just need to check if the package has a compatible target:
                if (package.PackageDetails.Targets.ContainsKey(target))
                {
                    compatibilityResult.Compatibility = Compatibility.COMPATIBLE;
                    return compatibilityResult;
                } else
                {
                    compatibilityResult.Compatibility = Compatibility.INCOMPATIBLE;
                }
                
                return compatibilityResult;
            }
        }


        public static CompatibilityResult GetCompatibilityResult(
            PackageDetailsWithApiIndices package, 
            string apiMethodSignature, 
            string packageVersion, 
            string target = "net6.0", 
            bool checkLesserPackage = false)
        {
            var compatibilityResult = new CompatibilityResult
            {
                Compatibility = Compatibility.UNKNOWN,
                CompatibleVersions = new List<string>()
            };

            // If necessary data to determine compatibility is missing, return unknown compatibility
            if (package == null 
                || apiMethodSignature == null 
                || !NuGetVersion.TryParse(packageVersion, out var validPackageVersion))
            {
                return compatibilityResult;
            }

            if (package.PackageDetails.IsDeprecated)
            {
                compatibilityResult.Compatibility = Compatibility.DEPRECATED;
                return compatibilityResult;
            }

            var apiDetails = GetApiDetails(package, apiMethodSignature);
            var compatiblePackageVersionsForTarget = 
                GetCompatiblePackageVersionsForTarget(apiDetails, package, target, checkLesserPackage);

            // If package version is greater than the greatest compatible version, it is likely this latest version
            // has not been assessed and added to the compatibility datastore. If it has a lower version of the same
            // major that is compatible, then it will be marked as Compatible. It will be marked as Incompatible otherwise
            var maxCompatibleVersion = NugetVersionHelper.GetMaxVersion(compatiblePackageVersionsForTarget);
            if (maxCompatibleVersion != null 
                && !maxCompatibleVersion.IsZeroVersion()
                && validPackageVersion.IsGreaterThan(maxCompatibleVersion))
            {
                compatibilityResult.Compatibility = validPackageVersion.HasSameMajorAs(maxCompatibleVersion)
                    ? Compatibility.COMPATIBLE
                    : Compatibility.INCOMPATIBLE;
            }
            // In all other cases, just check to see if the list of compatible versions for the target framework
            // contains the current package version
            else
            {
                compatibilityResult.Compatibility = validPackageVersion.HasLowerOrEqualCompatibleVersion(compatiblePackageVersionsForTarget)
                    ? Compatibility.COMPATIBLE
                    : Compatibility.INCOMPATIBLE;
            }

            // CompatibleVersions are recommended as potential upgrades from current version
            compatibilityResult.CompatibleVersions = validPackageVersion.FindGreaterCompatibleVersions(compatiblePackageVersionsForTarget).ToList();

            return compatibilityResult;
        }

        private static IEnumerable<string> GetCompatiblePackageVersionsForTarget(
            ApiDetails apiDetails, 
            PackageDetailsWithApiIndices package,
            string target,
            bool checkLesserPackage)
        {
            // If ApiDetails found, use them to get compatible versions
            if (apiDetails == null)
            {
                if (!checkLesserPackage
                    || package.PackageDetails.Targets == null
                    || !package.PackageDetails.Targets.TryGetValue(target, out var compatiblePackageVersionsForTarget))
                {
                    return new List<string>();
                }

                return compatiblePackageVersionsForTarget.ToList();
            }
            // If ApiDetails not found, fallback to using PackageDetails to get compatible versions
            else if (apiDetails.Targets.TryGetValue(target, out var compatiblePackageVersionsForTarget))
            {
                return compatiblePackageVersionsForTarget.ToList();
            }

            return new List<string>();
        }

        public static ApiRecommendation UpgradeStrategy(
            CompatibilityResult compatibilityResult,
            string apiMethodSignature,
            Task<RecommendationDetails> recommendationDetails,
            string target = "net6.0")
        {
            try
            {
                if (compatibilityResult?.CompatibleVersions != null)
                {
                    var validVersions = compatibilityResult.GetCompatibleVersionsWithoutPreReleases();
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
            Task<RecommendationDetails> 
            recommendationDetails,
            string target = "net6.0")
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
            if (packageDetailsWithApiIndices == null 
                || packageDetailsWithApiIndices.PackageDetails == null 
                || packageDetailsWithApiIndices.IndexDict == null 
                || packageDetailsWithApiIndices.PackageDetails.Api == null 
                || apiMethodSignature == null)
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
            var indexDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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
