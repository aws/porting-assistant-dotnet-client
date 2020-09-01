using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class SourceFileAnalysisResult
    {
        public string SourceFilePath { get; set; }
        public List<ApiAnalysisResult> ApiAnalysisResults { get; set; }
    }
}
