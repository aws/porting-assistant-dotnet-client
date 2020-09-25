using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PortingAssistant.Model
{
    public class PackageVersionPair
    {
        public string PackageId { get; set; }
        public string Version { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public PackageSourceType PackageSourceType { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PackageVersionPair pair &&
                   PackageId == pair.PackageId &&
                   Version == pair.Version;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PackageId, Version);
        }

        public override string ToString()
        {
            return $"{PackageId}-{Version}";
        }
    }
}
