

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PortingAssistant.Compatibility.Common.Model
{
    public class ApiEntity
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public CodeEntityType CodeEntityType { get; set; }
        public string Namespace { get; set; }
        public string OriginalDefinition { get; set; } //valid for method
    }
}
