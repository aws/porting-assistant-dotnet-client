using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortingAssistant.Client.Model
{
    public class SolutionAnalysisResult
    {
        public string Version { get; set; }
        public SolutionDetails SolutionDetails { get; set; }
        public List<ProjectAnalysisResult> ProjectAnalysisResults { get; set; }
        public List<string> FailedProjects { get; set; }
        public List<string> Errors { get; set; } //Solution errors; solution file errors etc.
    }
}
