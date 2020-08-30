using EncoreCommon.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace EncoreApiAnalysis.Model
{
    public class EncoreApiCompatibilityResponse
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public Compatibility Compatible { get; set; }
        public string MethodSignature { get; set; }
        public string Package { get; set; }
        public string Version { get; set; }
        public Upgrade Upgrades { get; set; }

        public override bool Equals(object obj)
        {
            return obj is EncoreApiCompatibilityResponse response &&
                   MethodSignature == response.MethodSignature &&
                   Compatible == response.Compatible &&
                   Package == response.Package &&
                   Version == response.Version;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MethodSignature, Compatible, Package, Version);
        }

        public class Upgrade
        {
            public string Newest { get; set; }
        }
    }
}
