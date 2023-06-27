using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Telemetry.Model
{
    public class NugetMetrics : MetricsBase
    {
        public string pacakgeName { get; set; }
        public string packageVersion { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Compatibility compatibility { get; set; }
        public string projectGuid { get; set; }
        public string solutionGuid { get; set; }
    }
}
