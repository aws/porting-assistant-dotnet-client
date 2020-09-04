using PortingAssistant.Model;

namespace PortingAssistant
{
    public interface IAssessmentHandler
    {
        SolutionDetails GetSolutionDetails(string solutionFilePath);
        SolutionAnalysisResult AnalyzeSolution(string solutionFilePath);
    }
}
