using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class PackageVersionResult
    {
        public string PackageId { get; set; }
        public string Version { get; set; }
        public Compatibility Compatible { get; set; }
        public List<string> packageUpgradeStrategies { get; set; }
    }
}
