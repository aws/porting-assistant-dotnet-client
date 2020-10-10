using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class ProjectApiAnalysisResult
    {
        public string SolutionFile { get; set; }
        public string ProjectFile { get; set; }
        public List<string> Errors { get; set; }
        public List<SourceFileAnalysisResult> SourceFileAnalysisResults { get; set; }
    }
}
