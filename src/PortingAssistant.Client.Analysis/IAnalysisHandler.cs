using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Analysis
{
    public interface IPortingAssistantAnalysisHandler
    {
        Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolution(string solutionFilename, List<string> projects);
    }
}
