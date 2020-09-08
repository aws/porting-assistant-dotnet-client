using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortingAssistant.Model
{
    public class ProjectAnalysisResult
    {
        public string ProjectName { get; set; }
        public string ProjectFile { get; set; }
        public Task<ProjectApiAnalysisResult> ProjectApiAnalysisResult;
        public List<Task<PackageAnalysisResult>> PackageAnalysisResults;
    }

    public class ProjectApiAnalysisResult
    {
        public List<string> Errors { get; set; }
        public List<SourceFileAnalysisResult> SourceFileAnalysisResults { get; set; }
    }
}
