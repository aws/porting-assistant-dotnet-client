using Codelyzer.Analysis;
using CTA.Rules.Models;
using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class IncrementalFileAnalysisResult : IncrementalAnalysisResult
    {
        public List<SourceFileAnalysisResult> sourceFileAnalysisResults { get; set; }
    }
}
