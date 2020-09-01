using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistantHandler.Model;
using PortingAssistant.Model;

namespace PortingAssistantHandler
{
    public interface IAssessmentHandler
    {
        SolutionDetails GetSolutionDetails(string solutionFilePath);
        SolutionAnalysisResult AnalyzeSolution(string solutionFilePath, AssessmentConfiguration Configuration);
    }
}
