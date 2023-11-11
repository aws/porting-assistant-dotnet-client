using Newtonsoft.Json;
using NuGet.Versioning;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using System.CodeDom.Compiler;
using System.IO.Compression;

namespace PortingAssistant.Compatibility.Common.Utils
{
    public static class ApiCompatiblity
    {
        private static CodeDomProvider? _codeDomProvider;
        private static CodeDomProvider CodeDomProvider
        {
            get
            { 
                _codeDomProvider ??= CodeDomProvider.CreateProvider("C#");
                return _codeDomProvider;
            }
        }

        private static readonly Recommendation DEFAULT_RECOMMENDATION = new Recommendation
        {
            RecommendedActionType = RecommendedActionType.NoRecommendation
        };

        public static Dictionary<ApiEntity, CompatibilityResult> IsCompatibleV2(
            KeyValuePair<PackageVersionPair, HashSet<ApiEntity>> packageWithApi,
            Dictionary<PackageVersionPair, PackageAnalysisResult> packageAnalysisCompatCheckerResults,
            Dictionary<PackageVersionPair, Task<PackageDetails>> sdkPackageResults,
            string targetFramework, IHttpService httpService, Language language = Language.CSharp)
        {
            var sdkPackageDetailsWithIndicesResults = PreProcessPackageDetails(sdkPackageResults);
            var package = packageWithApi.Key;

            Dictionary<ApiEntity, CompatibilityResult> apiCompatibilityResultDict = new Dictionary<ApiEntity, CompatibilityResult>();
            Dictionary<ApiEntity, CompatibilityResult> preProcessApiDetailsNugetPackage = null;
            CompatibilityResult? packageLevelCompatibleResult = null;
            if (packageAnalysisCompatCheckerResults.ContainsKey(package))
            {
                packageLevelCompatibleResult = packageAnalysisCompatCheckerResults[package].CompatibilityResults[targetFramework];
                preProcessApiDetailsNugetPackage = PreProcessApiDetailsNugetPackage(packageLevelCompatibleResult, packageWithApi, httpService, targetFramework, language).Result;
            }
            else {
                Console.WriteLine($"packageAnalysisCompatCheckerResults doesn't contain key {package}");
            }

            foreach (var api in packageWithApi.Value)
            {
                CompatibilityResult compatibilityResultWithPackage = new CompatibilityResult() {
                    Compatibility = Model.Compatibility.INCOMPATIBLE,
                    CompatibleVersions = packageLevelCompatibleResult == null ? 
                    new List<string>() : packageLevelCompatibleResult.CompatibleVersions,
                };

                // check result with nuget package
                if (preProcessApiDetailsNugetPackage!= null && preProcessApiDetailsNugetPackage.ContainsKey(api))
                {
                    //applied V2
                    compatibilityResultWithPackage = preProcessApiDetailsNugetPackage[api];
                };

                var sdkpackage = new PackageVersionPair
                { PackageId = api.Namespace, Version = "0.0.0", PackageSourceType = PackageSourceType.SDK };

                // potential check with namespace
                var sdkpackageDetails = sdkPackageDetailsWithIndicesResults.GetValueOrDefault(sdkpackage, null);
                var compatibilityResultWithSdk =
                    GetCompatibilityResult(sdkpackageDetails, api, package.Version, targetFramework);
                // Organize nuget and sdk api compatibility results. 
                var compatibilityResult =
                    GetApiCompatibilityResult(compatibilityResultWithPackage, compatibilityResultWithSdk);

                apiCompatibilityResultDict.Add(api, compatibilityResult);
            }
            return apiCompatibilityResultDict;
        }
        
        public static Dictionary<ApiEntity, CompatibilityResult> IsCompatible(
            KeyValuePair<PackageVersionPair, HashSet<ApiEntity>> packageWithApi,
            Dictionary<PackageVersionPair, Task<PackageDetails>> packageResults,
            string targetFramework)
        {
            var packageDetailsWithIndicesResults = PreProcessPackageDetails(packageResults);
            var package = packageWithApi.Key;

            Dictionary<ApiEntity, CompatibilityResult> apiCompatibilityResultDict = new Dictionary<ApiEntity, CompatibilityResult>();


            foreach (var api in packageWithApi.Value)
            {
                
                // check result with nuget package
                var packageDetails = packageDetailsWithIndicesResults.GetValueOrDefault(package, null);
                var compatibilityResultWithPackage =
                    GetCompatibilityResult(packageDetails, api, package.Version, targetFramework);

                var sdkpackage = new PackageVersionPair
                { PackageId = api.Namespace, Version = "0.0.0", PackageSourceType = PackageSourceType.SDK };

                // potential check with namespace
                var sdkpackageDetails = packageDetailsWithIndicesResults.GetValueOrDefault(sdkpackage, null);
                var compatibilityResultWithSdk =
                    GetCompatibilityResult(sdkpackageDetails, api, package.Version, targetFramework);
                // Organize nuget and sdk api compatibility results. 
                var compatibilityResult =
                    GetApiCompatibilityResult(compatibilityResultWithPackage, compatibilityResultWithSdk);

                apiCompatibilityResultDict.Add(api, compatibilityResult);
            }
            return apiCompatibilityResultDict;
        }
        


        public static CompatibilityResult GetApiCompatibilityResult(CompatibilityResult compatibilityResultWithPackage,
            CompatibilityResult compatibilityResultWithSdk)
        {
            var compatiblityResult = compatibilityResultWithPackage;

            switch (compatibilityResultWithPackage.Compatibility)
            {
                case Model.Compatibility.COMPATIBLE:
                    break;

                case Model.Compatibility.INCOMPATIBLE:
                    if (compatibilityResultWithSdk.Compatibility == Model.Compatibility.COMPATIBLE)
                    {
                        compatiblityResult = compatibilityResultWithSdk;
                    }

                    break;

                case Model.Compatibility.DEPRECATED:
                    if (compatibilityResultWithSdk.Compatibility == Model.Compatibility.COMPATIBLE ||
                        compatibilityResultWithSdk.Compatibility == Model.Compatibility.INCOMPATIBLE)
                    {
                        compatiblityResult = compatibilityResultWithSdk;
                    }

                    break;
                case Model.Compatibility.GENERAL_PARSE_ERROR:
                case Model.Compatibility.UNKNOWN:
                    if (compatibilityResultWithSdk.Compatibility == Model.Compatibility.COMPATIBLE ||
                        compatibilityResultWithSdk.Compatibility == Model.Compatibility.INCOMPATIBLE ||
                        compatibilityResultWithSdk.Compatibility == Model.Compatibility.DEPRECATED)
                    {
                        compatiblityResult = compatibilityResultWithSdk;
                    }

                    break;

            }

            return compatiblityResult;
        }

        public static CompatibilityResult GetCompatibilityResult(PackageDetailsWithApiIndices package, ApiEntity apiEntity, string packageVersion, string target = Constants.DefaultAssessmentTargetFramework, bool checkLesserPackage = false)
        {
            //If invocation, we will try to find it in a later package
            //Only CodeEntityType "Method" needs to be considered when check apis compatibility.
            if (apiEntity.CodeEntityType == CodeEntityType.Method)
            {
                if (string.IsNullOrEmpty(apiEntity.Namespace))
                {
                    // codelyzer was not able to parse the symbol from the semantic model, so we can't accurately assess the compatibility.
                    return new CompatibilityResult
                    {
                        Compatibility = Model.Compatibility.UNKNOWN,
                        CompatibleVersions = new List<string>()
                    };
                }
                return GetCompatibilityResult(package, apiEntity.OriginalDefinition, packageVersion, target, checkLesserPackage);
            }
            //If another node type, we will not try to find it. Compatibility will be based on the package compatibility
            else
            {
                var compatibilityResult = new CompatibilityResult
                {
                    Compatibility = Model.Compatibility.UNKNOWN,
                    CompatibleVersions = new List<string>()
                };

                if (package == null || !NuGetVersion.TryParse(packageVersion, out var targetversion))
                {
                    return compatibilityResult;
                }

                if (package.PackageDetails.IsDeprecated)
                {
                    compatibilityResult.Compatibility = Model.Compatibility.DEPRECATED;
                    return compatibilityResult;
                }

                //For other code entities, we just need to check if the package has a compatible target:
                if (package.PackageDetails.Targets.ContainsKey(target))
                {
                    compatibilityResult.Compatibility = Model.Compatibility.COMPATIBLE;
                    return compatibilityResult;
                }
                else
                {
                    compatibilityResult.Compatibility = Model.Compatibility.INCOMPATIBLE;
                }

                return compatibilityResult;
            }
        }

        public static async Task<Dictionary<ApiEntity,CompatibilityResult>> PreProcessApiDetailsNugetPackage(
            CompatibilityResult packageCompatibilityResult,
            KeyValuePair<PackageVersionPair, HashSet<ApiEntity>> packageWithApi,
            IHttpService httpService,
            string target = Constants.DefaultAssessmentTargetFramework,
            Language language = Language.CSharp)
        {
            Dictionary<ApiEntity, CompatibilityResult> result = new Dictionary<ApiEntity, CompatibilityResult> ();
            var defaultCompatibilityResult = new CompatibilityResult
            {
                Compatibility = Model.Compatibility.UNKNOWN,
                CompatibleVersions = new List<string>()
            };
            
            // If necessary data to determine compatibility is missing, return unknown compatibility
            if (packageCompatibilityResult == null)
            {
                Console.WriteLine($"Fail to find package level compatible result for {packageWithApi.Key.PackageId}.");
                foreach (var api in packageWithApi.Value)
                {
                    result.Add(api, defaultCompatibilityResult);
                }
                return result;
            }

            //if package level compatible result exist ; set all api compatible result default value to package compatible result
            defaultCompatibilityResult = packageCompatibilityResult;

            //if package is Incompatible, no need to check api file, all api will be incompatible and reuse the compatible version from package Level
            if (packageCompatibilityResult.Compatibility == Model.Compatibility.INCOMPATIBLE)
            {
                foreach (var api in packageWithApi.Value)
                {
                    result.Add(api, new CompatibilityResult
                    {
                        Compatibility = Model.Compatibility.INCOMPATIBLE,
                        CompatibleVersions = packageCompatibilityResult.CompatibleVersions
                    });
                }
                return result;
            }

            else if (packageCompatibilityResult.Compatibility == Model.Compatibility.COMPATIBLE)
            {
                var apidetails = await DownloadPackageVersionApiDetailsTask(packageWithApi.Key.PackageId, packageWithApi.Key.Version, httpService);
                // If no package -version-api-json.gz exists in s3, set all api to incompatible
                if (apidetails == null)
                {
                    Console.WriteLine($"No package detail find for {packageWithApi.Key}. Set all API to InCompatible");
                    foreach (var api in packageWithApi.Value)
                    {
                        result.Add(api, new CompatibilityResult
                        {
                            Compatibility = Model.Compatibility.INCOMPATIBLE,
                            CompatibleVersions = packageCompatibilityResult.CompatibleVersions
                        });
                    }
                    return result;
                }

                var apiIndexDict = SignatureToIndexPreProcess(apidetails);
                
                foreach (var api in packageWithApi.Value)
                {
                    ApiDetailsV2 selectedAPI = null;
                    
                    //if API methodSignature is not found in the apidetails, set this API to InCompatible
                    var index = apiIndexDict.GetValueOrDefault(api.OriginalDefinition, -1);

                    selectedAPI = index >= 0 && index < apidetails.Length ? apidetails[index] : null;
                    //for VB if methodSignature can not be found need to check again , remove all ?  from api.OriginalDefinition 
                    if (selectedAPI == null && language == Language.Vb)
                    {
                        var vbApiIndexDict = VBSignatureToIndexPreProcess(apidetails);
                        index = vbApiIndexDict.GetValueOrDefault(api.OriginalDefinition, -1);
                        selectedAPI = index >= 0 && index < apidetails.Length ? apidetails[index] : null;
                    }
                    
                    var compatibleResult = new CompatibilityResult
                    {
                        Compatibility = (selectedAPI!= null && !selectedAPI.IsCompatible) ? Model.Compatibility.INCOMPATIBLE : Model.Compatibility.COMPATIBLE,
                        CompatibleVersions = packageCompatibilityResult.CompatibleVersions
                    };
                    result.Add(api, compatibleResult);
                }
            }
            //if package level Compatibility is neither INCOMPATIBLE nor INCOMPATIBLE, rare case
            else
            {
                foreach (var api in packageWithApi.Value)
                {
                    result.Add(api, defaultCompatibilityResult);
                }
            }
            return result;

        }

        public static async Task<ApiDetailsV2[]?> DownloadPackageVersionApiDetailsTask(string packageName, string packageVersion, IHttpService httpService)
        {
            ApiDetailsV2[]? apiDetailsFromS3 = null;
            var packageVersionJsonKey = $"{packageName}/{packageName}-{packageVersion}-api.json.gz".ToLower();
            //currently we only support pre release version.  the version will be round up official version
            var packageVersionJsonKey_noPreRelease = $"{packageName}/{packageName}-{packageVersion.Split("-")[0]}-api.json.gz".ToLower();
            try
            {
                
                using var stream = await httpService.DownloadS3FileAsync(packageVersionJsonKey);
                if (stream != null)
                {
                    Console.WriteLine($"Found package version file {packageVersionJsonKey}.");
                    using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
                    using var streamReader = new StreamReader(gzipStream);
                    apiDetailsFromS3 = JsonConvert.DeserializeObject<ApiDetailsV2[]>(streamReader.ReadToEnd());
                    return apiDetailsFromS3;
                }
                else if (stream == null && !string.Equals(packageVersionJsonKey, packageVersionJsonKey_noPreRelease))
                {
                    using var stream2 = await httpService.DownloadS3FileAsync(packageVersionJsonKey_noPreRelease);
                    if (stream2 != null)
                    {
                        Console.WriteLine($"find non-PreRelease package version file {packageVersionJsonKey_noPreRelease}");
                        using var gzipStream2 = new GZipStream(stream2, CompressionMode.Decompress);
                        using var streamReader2 = new StreamReader(gzipStream2);
                        apiDetailsFromS3 = JsonConvert.DeserializeObject<ApiDetailsV2[]>(streamReader2.ReadToEnd());
                        return apiDetailsFromS3;
                    }
                    Console.WriteLine($"Not found non-PreReleasea package version file {packageVersionJsonKey_noPreRelease}.");
                }
            }
            catch(Exception ex)
            { 
                Console.WriteLine($"Fail to get packageVersion {packageVersionJsonKey} api details. Exception occurs: " +ex.Message);
            }
            return  apiDetailsFromS3;
        }

        public static CompatibilityResult GetCompatibilityResult(
            PackageDetailsWithApiIndices package,
            string apiMethodSignature,
            string packageVersion,
            string target = Constants.DefaultAssessmentTargetFramework,
            bool checkLesserPackage = false)
        {
            var compatibilityResult = new CompatibilityResult
            {
                Compatibility = Model.Compatibility.UNKNOWN,
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
                compatibilityResult.Compatibility = Model.Compatibility.DEPRECATED;
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
                    ? Model.Compatibility.COMPATIBLE
                    : Model.Compatibility.INCOMPATIBLE;
            }
            // In all other cases, just check to see if the list of compatible versions for the target framework
            // contains the current package version
            else
            {
                compatibilityResult.Compatibility = validPackageVersion.HasLowerOrEqualCompatibleVersion(compatiblePackageVersionsForTarget)
                    ? Model.Compatibility.COMPATIBLE
                    : Model.Compatibility.INCOMPATIBLE;
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

        public static Recommendation UpgradeStrategy(
            CompatibilityResult compatibilityResult,
            string apiMethodSignature,
            Task<RecommendationDetails> recommendationDetails,
            string target = Constants.DefaultAssessmentTargetFramework)
        {
            try
            {
                if (compatibilityResult?.CompatibleVersions != null)
                {
                    var validVersions = compatibilityResult.GetCompatibleVersionsWithoutPreReleases();
                    if (validVersions.Count != 0)
                    {
                        return new Recommendation
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
        
        private static Recommendation FetchApiRecommendation(
            string apiMethodSignature,
            Task<RecommendationDetails>
            recommendationDetails,
            string target = Constants.DefaultAssessmentTargetFramework)
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
                        return new Recommendation
                        {
                            RecommendedActionType = RecommendedActionType.ReplaceApi,
                            Description = string.Join(",", apiRecommendation.Where(recommend => !string.IsNullOrEmpty(recommend)))
                        };
                    }
                }
            }
            return DEFAULT_RECOMMENDATION;
        }

        public static ApiDetails GetApiDetails(PackageDetailsWithApiIndices packageDetailsWithApiIndices, string apiMethodSignature)
        {
            if (packageDetailsWithApiIndices == null
                || packageDetailsWithApiIndices.PackageDetails == null
                || packageDetailsWithApiIndices.IndexDict == null
                || packageDetailsWithApiIndices.PackageDetails.Api == null
                || apiMethodSignature == null)
            {
                return null;
            }


            var index = packageDetailsWithApiIndices.IndexDict.GetValueOrDefault(apiMethodSignature, -1);

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

        public static Dictionary<string, int> SignatureToIndexPreProcess(PackageDetails packageDetails)
        {
            var indexDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (packageDetails == null || packageDetails.Api == null)
            {
                return indexDict;
            }

            for (int i = 0; i < packageDetails.Api.Length; i++)
            {
                var api = packageDetails.Api[i];

                var signature = api.MethodSignature;

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

        public static Dictionary<string, int> SignatureToIndexPreProcess(ApiDetailsV2[] apiDetails)
        {
            var indexDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (apiDetails == null || apiDetails.Length == 0)
            {
                return indexDict;
            }

            for (int i = 0; i < apiDetails.Length; i++)
            {
                var api = apiDetails[i];

                var signature = api.methodSignature;

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

        public static Dictionary<string, int> VBSignatureToIndexPreProcess(ApiDetailsV2[] apiDetails)
        {
            var indexDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (apiDetails == null || apiDetails.Length == 0)
            {
                return indexDict;
            }

            for (int i = 0; i < apiDetails.Length; i++)
            {
                var api = apiDetails[i];

                var signature = api.methodSignature.Replace("?","");

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

        public static string RemoveParameterName(string methodParameter)
        {
            const string spaceChar = " ";
            var startOfParameterName = methodParameter.LastIndexOf(spaceChar);

            // If there are no spaces in the method parameter, no op
            if (startOfParameterName == -1)
            {
                return methodParameter;
            }

            var potentialParameterName = methodParameter.Substring(startOfParameterName);

            // If potential parameter name is valid, we want to remove it
            if (CodeDomProvider.IsValidIdentifier(potentialParameterName.Trim()))
            {
                return methodParameter.Remove(startOfParameterName);
            }
            
            // The potential parameter name was not a valid identifier, no op
            return methodParameter;
        }

        public static string GetExtensionSignature(ApiDetails api)
        {
            try
            {
                if (api == null || api.MethodParameters == null || api.MethodParameters.Length == 0)
                {
                    return null;
                }
                
                var methodParameters = api.MethodParameters.Select(RemoveParameterName).ToList();
                
                var possibleExtension = methodParameters[0];
                var methodSignatureIndex = api.MethodSignature.IndexOf("(") >= 0 ? api.MethodSignature.IndexOf("(") : api.MethodSignature.Length;
                var sliceMethodSignature = api.MethodSignature.Substring(0, methodSignatureIndex);
                var methodNameIndex = sliceMethodSignature.LastIndexOf(api.MethodName);
                var methodName = sliceMethodSignature.Substring(methodNameIndex >= 0 ? methodNameIndex : sliceMethodSignature.Length);
                var methodSignature = $"{possibleExtension}.{methodName}({String.Join(", ", methodParameters.Skip(1))})";
                return methodSignature;
            }
            catch
            {
                return null;
            }
        }
        
        public static string GetExtensionSignature(ApiDetailsV2 api)
        {
            try
            {
                if (api == null || api.methodParameters == null || api.methodParameters.Length == 0)
                {
                    return null;
                }

                var methodParameters = api.methodParameters.Select(RemoveParameterName).ToList();

                var possibleExtension = methodParameters[0];
                var methodSignatureIndex = api.methodSignature.IndexOf("(") >= 0 ? api.methodSignature.IndexOf("(") : api.methodSignature.Length;
                var sliceMethodSignature = api.methodSignature.Substring(0, methodSignatureIndex);
                var methodNameIndex = sliceMethodSignature.LastIndexOf(api.methodName);
                var methodName = sliceMethodSignature.Substring(methodNameIndex >= 0 ? methodNameIndex : sliceMethodSignature.Length);
                var methodSignature = $"{possibleExtension}.{methodName}({string.Join(", ", methodParameters.Skip(1))})";
                return methodSignature;
            }
            catch
            {
                return null;
            }
        }
    }
}
