using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Handler
{
    public interface IPortingAssistantHandler
    {
        Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionFilePath, Settings settings);
        List<PortingResult> ApplyPortingChanges(PortingRequest request);
    }
}
