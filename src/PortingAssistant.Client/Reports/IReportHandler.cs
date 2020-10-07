using PortingAssistant.Client.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortingAssistant.Client.Reports
{
    public interface IReportHandler
    {
        Task<bool> GenerateJsonReport(List<PortingResult> portingResults, string SolutionName, string outputFolder);
        Task<bool> GenerateJsonReport(SolutionDetails solutionDetails, string outputFolder);
        bool GenerateJsonReport(SolutionAnalysisResult solutionAnalysisResult, string outputFolder);
    }
}
