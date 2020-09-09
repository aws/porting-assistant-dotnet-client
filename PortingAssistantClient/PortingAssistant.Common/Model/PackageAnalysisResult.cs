using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class PackageAnalysisResult
    {
        public PackageVersionPair PackageVersionPair { get; set; }
        public Dictionary<string, CompatibilityResult> CompatibilityResult { get; set; } // Target Framework CompatibilityResult pair
        public Recommendations Recommendations { get; set; }
    }
}
