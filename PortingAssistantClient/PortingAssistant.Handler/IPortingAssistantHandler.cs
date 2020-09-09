using System.Collections.Generic;
using PortingAssistant.Model;

namespace PortingAssistant
{
    public interface IPortingAssistantHandler
    {
        SolutionDetails GetSolutionDetails(string solutionFilePath);
        SolutionAnalysisResult AnalyzeSolution(string solutionFilePath, Settings settings);
        List<PortingResult> ApplyPortingChanges(PortingRequest request);
    }
}
