using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PortingAssistant.Client.Model
{
    public class CodeEntityDetails
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public CodeEntityType CodeEntityType { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Signature { get; set; }  //valid for method
        public string OriginalDefinition { get; set; } //valid for method
        public TextSpan TextSpan { get; set; }
        public PackageVersionPair Package { get; set; }
    }
}
