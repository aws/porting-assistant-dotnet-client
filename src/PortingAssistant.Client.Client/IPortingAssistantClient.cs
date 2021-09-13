using System.Collections.Generic;
using System.Threading.Tasks;
using Codelyzer.Analysis;
using Codelyzer.Analysis.Model;
using CTA.Rules.Models;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Client
{
    public interface IPortingAssistantClient
    {
        Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionFilePath, AnalyzerSettings settings);
        IAsyncEnumerable<ProjectAnalysisResult> AnalyzeSolutionGeneratorAsync(string solutionFilePath, AnalyzerSettings settings);
        Task<List<SourceFileAnalysisResult>> AnalyzeFileAsync(string filePath, string projectFile, string solutionFilePath, 
            List<string> preportReferences, List<string> currentReferences, RootNodes rules, ExternalReferences externalReferences, AnalyzerSettings settings);
        Task<List<SourceFileAnalysisResult>> AnalyzeFileAsync(string filePath, string fileContent, string projectFile, string solutionFilePath,
            List<string> preportReferences, List<string> currentReferences, RootNodes rules, ExternalReferences externalReferences, AnalyzerSettings settings);
        List<PortingResult> ApplyPortingChanges(PortingRequest request);
    }
}
