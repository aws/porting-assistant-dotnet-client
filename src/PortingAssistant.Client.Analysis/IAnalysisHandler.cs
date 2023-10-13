using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Codelyzer.Analysis.Model;
using PortingAssistant.Client.Model;
using PortingAssistant.Compatibility.Common.Model;

namespace PortingAssistant.Client.Analysis
{
    public interface IPortingAssistantAnalysisHandler
    {

        Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolution(string solutionFilename, List<string> projects, string targetFramework = "net6.0", AnalyzerSettings settings = null,
            AssessmentType assessmentType = AssessmentType.FullAssessment);

        Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolution(
            string solutionFilename,
            List<string> projects,
            List<AnalyzerResult> analyzerResults,
            string targetFramework,
            AssessmentType assessmentType = AssessmentType.FullAssessment);

        Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolutionIncremental(string solutionFilename, List<string> projects,
            string targetFramework = "net6.0", AnalyzerSettings settings = null);

        Dictionary<string, ProjectAnalysisResult> GetCompatibilityResults(string solutionFilename, List<string> projects, List<AnalyzerResult> analyzerResults, string targetFramework = "net6.0");
        Dictionary<string, ProjectAnalysisResult> GetCompatibilityResultsIncremental(string solutionFilename, List<string> projects, List<AnalyzerResult> analyzerResults,
            string targetFramework = "net6.0");
        IAsyncEnumerable<ProjectAnalysisResult> AnalyzeSolutionGeneratorAsync(string solutionFilename, List<string> projects, 
            string targetFramework = "net6.0", [EnumeratorCancellation] CancellationToken ct = default);
    }
}
