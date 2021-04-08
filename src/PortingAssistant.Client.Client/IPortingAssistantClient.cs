using System.Collections.Generic;
using System.Threading.Tasks;
using Codelyzer.Analysis;
using CTA.Rules.Models;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Client
{
    public interface IPortingAssistantClient
    {
        Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionFilePath, AnalyzerSettings settings);
        Task<IncrementalFileAnalysisResult> AnalyzeFileAsync(List<string> filePaths, string solutionFilePath,
            List<AnalyzerResult> existingAnalyzerResults, Dictionary<string, ProjectActions> existingProjectActions, AnalyzerSettings settings);
        List<PortingResult> ApplyPortingChanges(PortingRequest request);
    }
}
