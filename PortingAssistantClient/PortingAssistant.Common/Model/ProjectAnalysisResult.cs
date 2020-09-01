using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class ProjectAnalysisResult
    {
        public string ProjectName { get; set; }
        public string ProjectFile { get; set; }
        public List<string> Errors { get; set; }
        public List<SourceFileAnalysisResult> SourceFileAnalysisResults;
        public List<PackageAnalysisResult> PackageAnalysisResults;
    }

    public class ProjectApiAnalysisResult
    {
        public List<string> Errors { get; set; }
        public List<SourceFileAnalysisResult> SourceFileAnalysisResults { get; set; }
    }
}
