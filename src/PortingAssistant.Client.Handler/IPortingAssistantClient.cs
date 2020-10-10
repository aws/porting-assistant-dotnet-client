using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Client
{
    public interface IPortingAssistantClient
    {
        SolutionDetails GetSolutionDetails(string solutionFilePath);
        Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionFilePath, PortingAssistantSettings settings);
        List<PortingResult> ApplyPortingChanges(PortingRequest request);
    }
}
