using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PortingAssistant.Compatibility.Common.Model
{
    public class CompatibilityResult
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public Compatibility Compatibility { get; set; }
        public List<string> CompatibleVersions { get; set; } = new();

        /// <summary>
        /// Returns list of compatible versions with and pre-release (alpha, beta, rc) versions filtered out
        /// </summary>
        public List<string> GetCompatibleVersionsWithoutPreReleases()
        {
            return CompatibleVersions.Where(v => !v.Contains("-")).ToList();
        }
    }
}