using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PortingAssistant.Compatibility.Common.Model
{
    public class PackageVersionPair
    {
        public string PackageId { get; set; }
        public string Version { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public PackageSourceType? PackageSourceType { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PackageVersionPair pair &&
                   PackageId == pair.PackageId &&
                   Version == pair.Version &&
                   PackageSourceType == pair.PackageSourceType;
        }

        public override int GetHashCode()
        {
            if (PackageSourceType== null)
            {
                return HashCode.Combine(PackageId, Version);
            }
            else
            {
                return HashCode.Combine(PackageId, Version, PackageSourceType);
            }
            
        }

        public override string ToString()
        {
            if (PackageSourceType== null)
            {
                return  $"{PackageId}-{Version}";
            }
            else{
                return $"{PackageId}-{Version}-{PackageSourceType.ToString()}";
            }
            
        }
    }
}
