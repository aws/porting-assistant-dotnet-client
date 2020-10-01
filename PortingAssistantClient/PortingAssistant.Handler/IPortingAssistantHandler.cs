using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Model;

namespace PortingAssistant.Handler
{
    public interface IPortingAssistantHandler
    {
        SolutionDetails GetSolutionDetails(string solutionFilePath);
        Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionFilePath, Settings settings);
        List<PortingResult> ApplyPortingChanges(PortingRequest request);
    }
}
