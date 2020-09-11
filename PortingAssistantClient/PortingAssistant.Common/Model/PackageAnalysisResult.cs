using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class PackageAnalysisResult
    {
        public PackageVersionPair PackageVersionPair { get; set; }
        public Dictionary<string, CompatibilityResult> CompatibilityResults { get; set; } // Target Framework CompatibilityResults pair
        public Recommendations Recommendations { get; set; }
    }
}
