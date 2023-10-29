

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PortingAssistant.Compatibility.Common.Model
{
    public class CompatibilityCheckerResponse: RecommendationOnlyResponse
    {
        public Dictionary<PackageVersionPair, AnalysisResult> PackageAnalysisResults { get; set; }
        public Dictionary<PackageVersionPair, Dictionary<string, AnalysisResult>> ApiAnalysisResults { get; set; }
    }

    // No need ApiAnalysisResults for RecommendationOnly Response
    public class RecommendationOnlyResponse
    {
        public string SolutionGUID { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Language Language { get; set; }
        public Dictionary<PackageVersionPair, AnalysisResult> PackageRecommendationResults { get; set; }
        public Dictionary<PackageVersionPair, Dictionary<string, AnalysisResult>> ApiRecommendationResults { get; set; }
        public bool HasError { get; set; } = false; 
        public Recommendations? GetRecommendationsForPackage(PackageVersionPair pkgVersionPair)
        {
            if (PackageRecommendationResults == null
                || !PackageRecommendationResults.TryGetValue(pkgVersionPair, out var pkgRecommendationResult))
            {
                return null;
            }

            return pkgRecommendationResult.Recommendations;
        }

        public Recommendations? GetRecommendationsForApi(PackageVersionPair pkgVersionPair, string methodSignature)
        {
            if (ApiRecommendationResults == null
                || !ApiRecommendationResults.TryGetValue(pkgVersionPair, out var apiRecommendationResultsForPackage)
                || !apiRecommendationResultsForPackage.TryGetValue(methodSignature, out var apiRecommendationResult)
               )
            {
                return null;
            }

            return apiRecommendationResult.Recommendations;
        }
    }
}
