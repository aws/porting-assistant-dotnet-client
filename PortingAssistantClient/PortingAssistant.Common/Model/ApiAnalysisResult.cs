using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class ApiAnalysisResult
    {
        public Invocation Invocation;
        public Compatibility CompatibilityResult { get; set; }
        public ApiRecommendation ApiRecommendation;
    }
}
