using System.Collections.Generic;
using PortingAssistant.Model;

namespace PortingAssistant.ApiAnalysis
{
    public interface IPortingAssistantApiAnalysisHandler
    {
        SolutionApiAnalysisResult AnalyzeSolution(string solutionFilename, List<Project> projects);
    }
}
