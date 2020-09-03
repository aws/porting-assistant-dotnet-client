using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class ApiAnalysisResult
    {
        public Invocation Invocation { get; set; }
        public Compatibility CompatibilityResult { get; set; }
        public ApiRecommendation ApiRecommendation { get; set; }
    }
}
