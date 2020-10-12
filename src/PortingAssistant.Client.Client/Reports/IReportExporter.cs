using PortingAssistant.Client.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortingAssistant.Client.Client.Reports
{
    public interface IReportExporter
    {
        bool GenerateJsonReport(List<PortingResult> portingResults, string SolutionName, string outputFolder);
        bool GenerateJsonReport(SolutionAnalysisResult solutionAnalysisResult, string outputFolder);
    }
}
