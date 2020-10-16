using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Client
{
    public interface IPortingAssistantClient
    {
        Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionFilePath, AnalyzerSettings settings);
        List<PortingResult> ApplyPortingChanges(PortingRequest request);
    }
}
