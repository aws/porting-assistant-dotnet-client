using Codelyzer.Analysis;
using CTA.Rules.Models;
using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class IncrementalAnalysisResult
    {
        public List<AnalyzerResult> analyzerResults { get; set; }
    }
}
