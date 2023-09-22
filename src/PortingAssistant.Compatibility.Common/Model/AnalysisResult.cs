

namespace PortingAssistant.Compatibility.Common.Model
{
    public class AnalysisResult
    {
        public Dictionary<string, CompatibilityResult> CompatibilityResults { get; set; } // Target Framework CompatibilityResults pair
        public Recommendations Recommendations { get; set; }
    }
}
