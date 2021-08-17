using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PortingAssistant.Client.Telemetry.Model
{
    public class MetricsBase
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MetricsType MetricsType { get; set; }
        public string PortingAssistantSource { get; set; }
        public string Version { get; set; }
        public string TargetFramework { get; set; }
        public string TimeStamp { get; set; }
    }
}
