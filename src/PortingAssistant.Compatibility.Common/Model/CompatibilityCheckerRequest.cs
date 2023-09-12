using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PortingAssistant.Compatibility.Common.Model
{
    public class CompatibilityCheckerRequest
    {
        public string TargetFramework { get; set; } 
        public string SolutionGUID { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Language Language { get; set; }
        public Dictionary<PackageVersionPair, HashSet<ApiEntity>> PackageWithApis { get; set; } 
        [JsonConverter(typeof(StringEnumConverter))]
        public AssessmentType AssessmentType{ get; set;}
    }

    public enum Language
    {
        CSharp,
        Vb
    }

    public enum AssessmentType
    {
        // Compatibility Checker results Only.  No need to append Recommandation result. 
        CompatibilityOnly, 
        FullAssessment,  
        RecommendationOnly
    }
}
