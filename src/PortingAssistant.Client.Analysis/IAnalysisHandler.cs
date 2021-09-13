using System.Collections.Generic;
using System.Threading.Tasks;
using Codelyzer.Analysis;
using Codelyzer.Analysis.Model;
using CTA.Rules.Models;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Analysis
{
    public interface IPortingAssistantAnalysisHandler
    {
        Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolution(string solutionFilename, List<string> projects, string targetFramework = "netcoreapp3.1");
        IAsyncEnumerable<ProjectAnalysisResult> AnalyzeSolutionGeneratorAsync(string solutionFilename, List<string> projects, string targetFramework = "netcoreapp3.1");

        Task<List<SourceFileAnalysisResult>> AnalyzeFileIncremental(string filePath, string projectFile, string solutionFileName, List<string> preportReferences
            , List<string> currentReferences, RootNodes rules, ExternalReferences externalReferences, bool actionsOnly, bool compatibleOnly, string targetFramework = "netcoreapp3.1");
        Task<List<SourceFileAnalysisResult>> AnalyzeFileIncremental(string filePath, string fileContent, string projectFile, string solutionFileName, List<string> preportReferences
    , List<string> currentReferences, RootNodes rules, ExternalReferences externalReferences, bool actionsOnly, bool compatibleOnly, string targetFramework = "netcoreapp3.1");

        Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolutionIncremental(string solutionFilename, List<string> projects,
            string targetFramework = "netcoreapp3.1");
    }
}
