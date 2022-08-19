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
        Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolution(string solutionFilename, List<string> projects, string targetFramework = "net6.0", string msBuildPath = null, List<string> msBuildArguments = null);
        Task<List<SourceFileAnalysisResult>> AnalyzeFileIncremental(string filePath, string projectFile, string solutionFileName, List<string> preportReferences
            , List<string> currentReferences, RootNodes rules, ExternalReferences externalReferences, bool actionsOnly, bool compatibleOnly, string targetFramework = "net6.0");
        Task<List<SourceFileAnalysisResult>> AnalyzeFileIncremental(string filePath, string fileContent, string projectFile, string solutionFileName, List<string> preportReferences
    , List<string> currentReferences, RootNodes rules, ExternalReferences externalReferences, bool actionsOnly, bool compatibleOnly, string targetFramework = "net6.0");

        Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolutionIncremental(string solutionFilename, List<string> projects,
            string targetFramework = "net6.0", string msBuildPath = null, List<string> msBuildArguments = null);

        IAsyncEnumerable<ProjectAnalysisResult> AnalyzeSolutionGeneratorAsync(string solutionFilename, List<string> projects, string targetFramework = "net6.0");

        Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolutionUsingVSWorkspace(string solutionFilename, List<string> projects, string targetFramework = "net6.0", string msBuildPath = null, List<string> msBuildArguments = null, string workspace = null);
        Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolutionIncrementalUsingVSWorkspace(string solutionFilename, List<string> projects,
           string targetFramework = "net6.0", string msBuildPath = null, List<string> msBuildArguments = null, string workspace = null);
    }
}
