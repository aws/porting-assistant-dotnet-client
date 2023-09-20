using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using PortingAssistant.Compatibility.Common.Utils;
using Microsoft.Extensions.Logging;

namespace PortingAssistant.Compatibility.Core
{
	public class CompatibilityCheckerHandler: ICompatibilityCheckerHandler
    {
        private ICompatibilityCheckerNuGetHandler _nuGetHandler;
        private ICompatibilityCheckerRecommendationHandler _recommendationHandler;
        private IHttpService _httpService;
        private readonly ILogger _logger;

        public CompatibilityCheckerHandler(
            ICompatibilityCheckerNuGetHandler nuGetHandler,
            ICompatibilityCheckerRecommendationHandler recommendationHandler,
            IHttpService httpService,
            ILogger<CompatibilityCheckerHandler> logger)
		{
            _nuGetHandler = nuGetHandler;
            _recommendationHandler = recommendationHandler;
            _httpService = httpService;
            _logger = logger;
        }

        public async Task<CompatibilityCheckerResponse> Check(CompatibilityCheckerRequest request, HashSet<string> fullSdks) //ILambdaLogger logger 
        {
            var language = request.Language;
            var targetFramework = request.TargetFramework ?? Constants.DefaultAssessmentTargetFramework;
            var solutionGuid = request.SolutionGUID;
            var packageWithApis = request.PackageWithApis;

            Dictionary<PackageVersionPair, PackageAnalysisResult> packageAnalysisCompatCheckerResults =
                new Dictionary<PackageVersionPair, PackageAnalysisResult>();
            Dictionary<PackageVersionPair, Dictionary<string, AnalysisResult>> apiAnalysisCompatCheckerResults =
                new Dictionary<PackageVersionPair, Dictionary<string, AnalysisResult>>();

            var allPackages = packageWithApis.Keys.ToHashSet();

            if (!allPackages.Any())
            {
                return new CompatibilityCheckerResponse();
            }

            // Get SDK and NuGet package details from the datastore. 
            Dictionary<PackageVersionPair, Task<PackageDetails>> packageDetailsDict =
                new Dictionary<PackageVersionPair, Task<PackageDetails>>();
            packageDetailsDict = _nuGetHandler.GetNugetPackages(allPackages.ToList());

            var sdkPackageDetailsDict = packageDetailsDict.Where(s => s.Key.PackageSourceType == PackageSourceType.SDK)
                            .ToDictionary(dict => dict.Key, dict => dict.Value);

            // Compatibility and recommendation results for each packageWithApi.
            foreach (var packageWithApi in packageWithApis)
            {
                var namespaces = new HashSet<string>();
                var originalDefinition = new HashSet<string>();

                foreach (var apiEntity in packageWithApi.Value)
                {
                    namespaces.Add(apiEntity.Namespace);
                    originalDefinition.Add(apiEntity.OriginalDefinition);
                }
                // Package level.
                if (packageWithApi.Key.PackageSourceType == PackageSourceType.NUGET)
                {
                    var nugetPackage = packageWithApi.Key;
                    // Get NuGet package level compatibility result. 
                    var nugetPackageCompatibilityResult = await PackageCompatibility.IsCompatibleAsync(
                        packageDetailsDict.GetValueOrDefault(nugetPackage, null),
                        nugetPackage, _logger, targetFramework);

                    var packageAnalysisResult = PackageCompatibility.GetPackageAnalysisResult(nugetPackageCompatibilityResult,
                        nugetPackage, targetFramework, request.AssessmentType);
                    packageAnalysisCompatCheckerResults[packageWithApi.Key] =
                        packageAnalysisResult;
                }

                // API level.
                // Get API level compatibility result.
                Dictionary<string, AnalysisResult> apiMethodAnalysisResultDict = new Dictionary<string, AnalysisResult>();
                
                var apiCompatibilityResultDict =
                    ApiCompatiblity.IsCompatibleV2(packageWithApi, packageAnalysisCompatCheckerResults, sdkPackageDetailsDict, targetFramework, _httpService, language);


                // Fetch Api recommendations.
                var apiAnalysisResult = new AnalysisResult();
                Dictionary<string, Task<RecommendationDetails>> apiRecommendationResults=
                    apiRecommendationResults = _recommendationHandler.GetApiRecommendation(namespaces.ToList());
                
                foreach (var api in apiCompatibilityResultDict)
                {
                    var packageResult = packageAnalysisCompatCheckerResults.GetValueOrDefault(packageWithApi.Key, null);
                    var recommendationDetails = apiRecommendationResults.GetValueOrDefault(api.Key.Namespace, null);
                    var apiRecommendation = ApiCompatiblity.UpgradeStrategy(apiCompatibilityResultDict[api.Key],
                        api.Key.OriginalDefinition, recommendationDetails, targetFramework);

                    switch (request.AssessmentType) {
                        case AssessmentType.CompatibilityOnly:
                            apiAnalysisResult = new AnalysisResult()
                            {
                                CompatibilityResults = new Dictionary<string, CompatibilityResult>
                                {
                                    { targetFramework, apiCompatibilityResultDict[api.Key] }
                                }
                            };
                            break;
                        case AssessmentType.RecommendationOnly:
                            apiAnalysisResult = new AnalysisResult()
                            {
                                Recommendations = new Recommendations
                                {
                                    RecommendedActions = new List<Recommendation>
                                    {
                                        apiRecommendation
                                    },
                                    RecommendedPackageVersions = packageResult?.CompatibilityResults[request.TargetFramework]?.CompatibleVersions
                                }
                            };
                            break;
                        case AssessmentType.FullAssessment:

                            apiAnalysisResult = new AnalysisResult()
                            {
                                CompatibilityResults = new Dictionary<string, CompatibilityResult>
                                {
                                    { targetFramework, apiCompatibilityResultDict[api.Key] }
                                },

                                Recommendations = new Recommendations
                                {
                                    RecommendedActions = new List<Recommendation>
                                    {
                                        apiRecommendation
                                    },
                                    RecommendedPackageVersions = packageResult?.CompatibilityResults[request.TargetFramework]?.CompatibleVersions
                                }
                            };
                        break;
                    }
                    apiMethodAnalysisResultDict.TryAdd(api.Key.OriginalDefinition, apiAnalysisResult);
                }
                
                // add api result to dictionary
                if (!apiAnalysisCompatCheckerResults.ContainsKey(packageWithApi.Key))
                {
                    apiAnalysisCompatCheckerResults.TryAdd(packageWithApi.Key, apiMethodAnalysisResultDict);
                }
                else
                {
                    apiAnalysisCompatCheckerResults[packageWithApi.Key] = apiMethodAnalysisResultDict;
                }
            }

            Dictionary<PackageVersionPair, AnalysisResult> packageAnalysisResults =
                new Dictionary<PackageVersionPair, AnalysisResult>();
            foreach (var p in packageAnalysisCompatCheckerResults)
            {
                switch (request.AssessmentType) {
                    case AssessmentType.CompatibilityOnly:
                        packageAnalysisResults.TryAdd(p.Key,
                        new AnalysisResult
                        {
                            CompatibilityResults = p.Value.CompatibilityResults
                        });
                        break;
                    case AssessmentType.RecommendationOnly:
                        packageAnalysisResults.TryAdd(p.Key,
                        new AnalysisResult
                        {
                            Recommendations = p.Value.Recommendations,
                        });
                        break;
                    default:
                        packageAnalysisResults.TryAdd(p.Key,
                        new AnalysisResult
                        {
                            Recommendations = p.Value.Recommendations,
                            CompatibilityResults = p.Value.CompatibilityResults
                        });
                        break;
                }
            }
            return new CompatibilityCheckerResponse()
            {
                SolutionGUID = solutionGuid,
                Language = language,
                PackageAnalysisResults = packageAnalysisResults,
                ApiAnalysisResults = apiAnalysisCompatCheckerResults,
                PackageRecommendationResults = packageAnalysisResults,
                ApiRecommendationResults = apiAnalysisCompatCheckerResults
            };
        }


        public async Task<CompatibilityCheckerResponse> Check(CompatibilityCheckerRequest request)
        {
            return await Check(request, null);
        }

        public static CompatibilityResult GetCompatibilityResult(CompatibilityResult compatibilityResultWithPackage,
            CompatibilityResult compatibilityResultWithSdk)
        {
            var compatiblityResult = compatibilityResultWithPackage;

            switch (compatibilityResultWithPackage.Compatibility)
            {
                case Common.Model.Compatibility.COMPATIBLE:
                    break;

                case Common.Model.Compatibility.INCOMPATIBLE:
                    if (compatibilityResultWithSdk.Compatibility == Common.Model.Compatibility.COMPATIBLE)
                    {
                        compatiblityResult = compatibilityResultWithSdk;
                    }
                    break;

                case Common.Model.Compatibility.DEPRECATED:
                    if (compatibilityResultWithSdk.Compatibility == Common.Model.Compatibility.COMPATIBLE ||
                        compatibilityResultWithSdk.Compatibility == Common.Model.Compatibility.INCOMPATIBLE)
                    {
                        compatiblityResult = compatibilityResultWithSdk;
                    }
                    break;

                case Common.Model.Compatibility.UNKNOWN:
                    if (compatibilityResultWithSdk.Compatibility == Common.Model.Compatibility.COMPATIBLE ||
                        compatibilityResultWithSdk.Compatibility == Common.Model.Compatibility.INCOMPATIBLE ||
                        compatibilityResultWithSdk.Compatibility == Common.Model.Compatibility.DEPRECATED)
                    {
                        compatiblityResult = compatibilityResultWithSdk;
                    }
                    break;

                default:
                    break;
            }

            return compatiblityResult;
        }

    }
}

