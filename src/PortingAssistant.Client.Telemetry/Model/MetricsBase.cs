using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PortingAssistant.Client.Telemetry.Model
{
    public class MetricsBase
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MetricsType metricsType { get; set; }
        public string portingAssistantSource { get; set; }
        public string tag { get; set; }
        public string version { get; set; }
        public string targetFramework { get; set; }
        public string timeStamp { get; set; }
    }
}
