
using CTA.Rules.Models;
using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class IncrementalProjectAnalysisResultDict : IncrementalAnalysisResult
    {
        public Dictionary<string, ProjectAnalysisResult> projectAnalysisResultDict { get; set; }
    }
}
