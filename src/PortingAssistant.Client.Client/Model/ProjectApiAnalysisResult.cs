using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class ProjectApiAnalysisResult
    {
        public string SchemaVersion { get; set; }
        public string SolutionFile { get; set; }
        public string SolutionGuid { get; set; }
        public string ApplicationGuid { get; set; }
        public string RepositoryUrl { get; set; }
        public string ProjectFile { get; set; }
        public string ProjectGuid { get; set; }
        public List<string> Errors { get; set; }
        public List<SourceFileAnalysisResult> SourceFileAnalysisResults { get; set; }
    }
}
