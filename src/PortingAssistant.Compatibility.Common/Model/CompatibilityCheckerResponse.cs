

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PortingAssistant.Compatibility.Common.Model
{
    public class CompatibilityCheckerResponse: RecommendationOnlyResponse
    {
        public Dictionary<PackageVersionPair, AnalysisResult> PackageAnalysisResults { get; set; }
        public Dictionary<PackageVersionPair, Dictionary<string, AnalysisResult>> ApiAnalysisResults { get; set; }
    }

    //no need ApiAnalysisResults for  RecommendationOnly Response
    public class RecommendationOnlyResponse
    {
        public string SolutionGUID { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Language Language { get; set; }
        public Dictionary<PackageVersionPair, AnalysisResult> PackageRecommendationResults { get; set; }
        public Dictionary<PackageVersionPair, Dictionary<string, AnalysisResult>> ApiRecommendationResults { get; set; }
    }
}
