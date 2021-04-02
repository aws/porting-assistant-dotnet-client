using CTA.Rules.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortingAssistant.Client.Model
{
    public class IncrementalSolutionAnalysisResult : IncrementalAnalysisResult
    {
        public SolutionAnalysisResult solutionAnalysisResult { get; set; }
        public Dictionary<string, ProjectActions> projectActions { get; set; }
    }
}
