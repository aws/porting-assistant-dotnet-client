using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortingAssistant.Client.Model
{
    public class ProjectAnalysisResult
    {
        public string ProjectName { get; set; }
        public string ProjectFile { get; set; }
        public List<string> TargetFrameworks { get; set; }
        public List<string> ProjectReferences { get; set; }
        public List<PackageVersionPair> PackageReferences { get; set; }
        public List<string> Errors { get; set; }
        public bool IsBuildFailed { get; set; }
        public List<SourceFileAnalysisResult> SourceFileAnalysisResults { get; set; }
        public Dictionary<PackageVersionPair, Task<PackageAnalysisResult>> PackageAnalysisResults { get; set; }
    }
}
