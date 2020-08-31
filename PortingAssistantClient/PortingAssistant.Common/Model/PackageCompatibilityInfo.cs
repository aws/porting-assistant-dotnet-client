using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class PackageCompatibilityInfo
    {
        Compatibility CompatibilityResult { get; set; }
        SortedSet<string> CompatibleVersion { get; set; }
    }
}
