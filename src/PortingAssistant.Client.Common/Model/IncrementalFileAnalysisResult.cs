using CTA.Rules.Models;
using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class IncrementalFileAnalysisResult : IncrementalAnalysisResult
    {
        public List<SourceFileAnalysisResult> sourceFileAnalysisResults { get; set; }
        public ProjectActions projectActions { get; set; }
    }
}
