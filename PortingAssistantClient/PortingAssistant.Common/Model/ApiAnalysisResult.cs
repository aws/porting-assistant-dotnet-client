using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class ApiAnalysisResult
    {
        public CodeEntityDetails CodeEntityDetails { get; set; }
        public Dictionary<string, Compatibility> CompatibilityResult { get; set; } // Target Framework CompatibilityResult pair
        public ApiRecommendation ApiRecommendation { get; set; }
    }
}
