using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PortingAssistant.Client.Model
{
    public class CompatibilityResult
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public Compatibility Compatibility { get; set; }
        public List<string> CompatibleVersions { get; set; }
    }
}
