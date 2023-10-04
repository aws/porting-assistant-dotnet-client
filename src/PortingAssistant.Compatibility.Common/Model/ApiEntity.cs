

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
        
        public override bool Equals(object obj)
        {
            return obj is ApiEntity entity &&
                   CodeEntityType == entity.CodeEntityType &&
                   Namespace == entity.Namespace &&
                   OriginalDefinition == entity.OriginalDefinition;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CodeEntityType, Namespace, OriginalDefinition);
        }
    }
}
