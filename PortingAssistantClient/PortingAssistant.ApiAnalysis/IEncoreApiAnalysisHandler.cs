using System.Collections.Generic;
using PortingAssistant.Model;

namespace PortingAssistant.ApiAnalysis
{
    public interface IPortingAssistantApiAnalysisHandler
    {
        SolutionAnalysisResult AnalyzeSolution(string solutionFilename, List<Project> projects);
    }
}
