using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Codelyzer.Analysis.Model;
using CTA.Rules.Models;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Client
{
    public interface IPortingAssistantClient
    {
        Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionFilePath, AnalyzerSettings settings);
        IAsyncEnumerable<ProjectAnalysisResult> AnalyzeSolutionGeneratorAsync(string solutionFilePath, AnalyzerSettings settings, [EnumeratorCancellation] CancellationToken cancellationToken = default);
        Task<List<SourceFileAnalysisResult>> AnalyzeFileAsync(string filePath, string projectFile, string solutionFilePath, 
            List<string> preportReferences, List<string> currentReferences, RootNodes rules, ExternalReferences externalReferences, AnalyzerSettings settings);
        Task<List<SourceFileAnalysisResult>> AnalyzeFileAsync(string filePath, string fileContent, string projectFile, string solutionFilePath,
            List<string> preportReferences, List<string> currentReferences, RootNodes rules, ExternalReferences externalReferences, AnalyzerSettings settings);
        List<PortingResult> ApplyPortingChanges(PortingRequest request);
    }
}
