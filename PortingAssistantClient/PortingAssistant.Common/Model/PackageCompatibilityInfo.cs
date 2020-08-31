using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class PackageCompatibilityInfo
    {
        public Compatibility CompatibilityResult { get; set; }
        public List<string> CompatibleVersion { get; set; }
    }
}
