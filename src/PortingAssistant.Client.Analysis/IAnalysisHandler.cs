using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Analysis
{
    public interface IPortingAssistantAnalysisHandler
    {
        Dictionary<string, Task<ProjectAnalysisResult>> AnalyzeSolution(string solutionFilename, List<ProjectDetails> projects);
    }
}
