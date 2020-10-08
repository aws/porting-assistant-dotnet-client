using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class SourceFileAnalysisResult
    {
        public string SourceFileName { get; set; }
        public string SourceFilePath { get; set; }
        public List<ApiAnalysisResult> ApiAnalysisResults { get; set; }
    }
}
