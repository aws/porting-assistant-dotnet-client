using System.Collections.Generic;
using System.Threading.Tasks;

namespace EncoreCommon.Model
{
    public class SolutionAnalysisResult
    {
        public Dictionary<string, Task<ProjectAnalysisResult>> ProjectAnalysisResults { get; set; }
    }
}
