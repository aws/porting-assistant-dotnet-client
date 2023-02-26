using Microsoft.CodeAnalysis;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static System.Net.Mime.MediaTypeNames;

using System.Security.Cryptography;

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
        public string SessionId { get; set; }
        // Application id is the hash of(solution path + mac id)
        public string ApplicationId { get; set; }
    }
}
