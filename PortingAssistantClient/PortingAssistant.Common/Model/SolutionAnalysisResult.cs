using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortingAssistant.Model
{
    public class SolutionAnalysisResult
    {
        public Dictionary<string, Task<ProjectAnalysisResult>> ProjectAnalysisResults { get; set; }
    }
}
