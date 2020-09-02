using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortingAssistant.Model
{
    public class SolutionAnalysisResult
    {
        public string Version { get; set; }
        public SolutionDetails SolutionDetails { get; set; }
        public List<ProjectAnalysisResult> ProjectAnalysisResult { get; set; }
        public List<string> FailedProjects { get; set; }
        public List<string> Errors { get; set; } //Solution errors; solution file errors etc.
    }

    public class SolutionApiAnalysisResult
    {
        public Dictionary<string, Task<ProjectApiAnalysisResult>> ProjectApiAnalysisResults { get; set; }
    }


}
