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
        //Task<IncrementalFileAnalysisResult> AnalyzeFileIncremental(List<string> filePath, string solutionFileName, List<AnalyzerResult> existingAnalyzerResults
        //    , Dictionary<string, ProjectActions> existingProjectActions, string targetFramework = "netcoreapp3.1");
        Task<IncrementalFileAnalysisResult> AnalyzeFileIncremental(string filePath, string projectFile, string solutionFileName, List<string> preportReferences
            , List<string> currentReferences, RootNodes rules, ExternalReferences externalReferences, bool actionsOnly, string targetFramework = "netcoreapp3.1");
        Task<IncrementalFileAnalysisResult> AnalyzeFileIncremental(string filePath, string fileContent, string projectFile, string solutionFileName, List<string> preportReferences
    , List<string> currentReferences, RootNodes rules, ExternalReferences externalReferences, bool actionsOnly, string targetFramework = "netcoreapp3.1");

        Task<IncrementalProjectAnalysisResultDict> AnalyzeSolutionIncremental(string solutionFilename, List<string> projects,
            string targetFramework = "netcoreapp3.1");
    }
}
