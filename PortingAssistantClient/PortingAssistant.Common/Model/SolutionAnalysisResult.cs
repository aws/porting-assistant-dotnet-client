using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortingAssistant.Model
{
    public class SolutionAnalysisResults
    {
        private string Version { get; set; }
        public SolutionDetails SolutionDetails { get; set; }
        public List<ProjectAnalysisResult> ProjectAnalysisResult { get; set; }
        public List<Project> FailedProjects { get; set; }
        public List<string> Errors { get; set; } //Solution errors; solution file errors etc.
    }

    public class SolutionAnalysisResult
    {
        public Dictionary<string, Task<ProjectAnalysisResult>> ProjectAnalysisResults { get; set; }
    }


}
