using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace PortingAssistant.Compatibility.Common.Model
{
    public class Recommendations
    {
        public List<Recommendation> RecommendedActions { get; set; }
        public List<string> RecommendedPackageVersions { get; set; }

    }

    public class Recommendation
    {
        public string PackageId { get; set; }
        public string Version { get; set; }
        public List<string> TargetVersions { get; set; }
        public string Description { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public RecommendedActionType RecommendedActionType { get; set; }
    }

    public enum RecommendedActionType
    {
        UpgradePackage,
        ReplaceApi,
        //Future
        ReplaceNamespace,
        ReplacePackage,
        NoRecommendation
    }
}
