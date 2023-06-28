using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Telemetry.Model
{
    public class APIMetrics : MetricsBase
    {
        public string name { get; set; }
        public string nameSpace { get; set; }
        public string originalDefinition { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Compatibility compatibility { get; set; }
        public string packageId { get; set; }
        public string packageVersion { get; set; }
        public string projectGuid { get; set; }
        public string solutionGuid { get; set; }
    }
}
