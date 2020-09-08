using System.Collections.Generic;
using PortingAssistant.Model;

namespace PortingAssistant
{
    public interface IPortingAssistantHandler
    {
        SolutionDetails GetSolutionDetails(string solutionFilePath);
        SolutionAnalysisResult AnalyzeSolution(string solutionFilePath);
        List<PortingProjectFileResult> ApplyPortingProjectFileChanges(ApplyPortingProjectFileChangesRequest request);
    }
}
