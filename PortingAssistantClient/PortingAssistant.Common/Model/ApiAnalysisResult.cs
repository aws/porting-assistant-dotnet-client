using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class ApiAnalysisResult
    {
        public CodeEntityDetails CodeEntityDetails { get; set; }
        public Dictionary<string, Compatibility> CompatibilityResults { get; set; } // Target Framework CompatibilityResults pair
        public Recommendations Recommendations { get; set; }
    }
}
