using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Model;

namespace PortingAssistant.Analysis
{
    public interface IPortingAssistantAnalysisHandler
    {
        Dictionary<string, Task<ProjectAnalysisResult>> AnalyzeSolution(string solutionFilename, List<ProjectDetails> projects);
    }
}
