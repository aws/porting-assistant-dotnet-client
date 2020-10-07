using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PortingAssistant.Client.Model
{
    public class RecommendedAction
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RecommendedActionType RecommendedActionType { get; set; }
        public string Description { get; set; }
    }
}
