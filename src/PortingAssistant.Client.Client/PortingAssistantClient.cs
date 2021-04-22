using System;
using System.Collections.Generic;
using System.Linq;
using PortingAssistant.Client.Analysis;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.Porting;
using Microsoft.Build.Construction;
using System.IO;
using System.Threading.Tasks;
using Codelyzer.Analysis;
using CTA.Rules.Models;
using Codelyzer.Analysis.Model;

namespace PortingAssistant.Client.Client
{
    public class PortingAssistantClient : IPortingAssistantClient
    {
        private readonly IPortingAssistantAnalysisHandler _analysisHandler;
        private readonly IPortingHandler _portingHandler;

        public PortingAssistantClient(IPortingAssistantAnalysisHandler AnalysisHandler,
            IPortingHandler portingHandler)
        {
            _analysisHandler = AnalysisHandler;
            _portingHandler = portingHandler;
        }

        public async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionFilePath, AnalyzerSettings settings)
        {
            try
            {
                var solution = SolutionFile.Parse(solutionFilePath);
                var failedProjects = new List<string>();

                var projects = solution.ProjectsInOrder.Where(p =>
                    (p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat ||
                    p.ProjectType == SolutionProjectType.WebProject) &&
                    (settings.IgnoreProjects?.Contains(p.AbsolutePath) != true))
                    .Select(p => p.AbsolutePath)
                    .ToList();

                var targetFramework = settings.TargetFramework ?? "netcoreapp3.1";

                Dictionary<string, ProjectAnalysisResult> projectAnalysisResultsDict;
                List<AnalyzerResult> analyzerResults = null;
                Dictionary<string, ProjectActions> projectActions = null;

                if (settings.ContiniousEnabled)
                {
                    var incrementalSolutionResult = await _analysisHandler.AnalyzeSolutionIncremental(solutionFilePath, projects, targetFramework);
                    projectAnalysisResultsDict = incrementalSolutionResult.projectAnalysisResultDict;
                    analyzerResults = incrementalSolutionResult.analyzerResults;
                    projectActions = incrementalSolutionResult.projectActions;
                }
                else
                    projectAnalysisResultsDict = await _analysisHandler.AnalyzeSolution(solutionFilePath, projects, targetFramework);

                var projectAnalysisResults = projects.Select(p =>
                {
                    var projectAnalysisResult = projectAnalysisResultsDict.GetValueOrDefault(p, null);
                    if (projectAnalysisResult != null)
                    {
                        if (projectAnalysisResult.IsBuildFailed)
                        {
                            failedProjects.Add(p);
                        }
                        return projectAnalysisResult;
                    }
                    return null;
                }).Where(p => p != null).ToList();

                var solutionDetails = new SolutionDetails
                {
                    SolutionName = Path.GetFileNameWithoutExtension(solutionFilePath),
                    SolutionFilePath = solutionFilePath,
                    Projects = projectAnalysisResults.ConvertAll(p => new ProjectDetails
                    {
                        PackageReferences = p.PackageReferences,
                        ProjectFilePath = p.ProjectFilePath,
                        ProjectGuid = p.ProjectGuid,
                        ProjectName = p.ProjectName,
                        ProjectReferences = p.ProjectReferences,
                        ProjectType = p.ProjectType,
                        TargetFrameworks = p.TargetFrameworks,
                        IsBuildFailed = p.IsBuildFailed
                    }),

                    FailedProjects = failedProjects
                };


                return new SolutionAnalysisResult
                {
                    FailedProjects = failedProjects,
                    SolutionDetails = solutionDetails,
                    ProjectAnalysisResults = projectAnalysisResults,
                    AnalyzerResults = analyzerResults,
                    ProjectActions = projectActions
                };

            }
            catch (Exception ex)
            {
                throw new PortingAssistantException($"Cannot Analyze solution {solutionFilePath}", ex);
            }

        }

        public async Task<IncrementalFileAnalysisResult> AnalyzeFileAsync(string filePath, string projectFile, string solutionFilePath,
    List<string> preportReferences, List<string> currentReferences, RootNodes rules, ExternalReferences externalReferences, AnalyzerSettings settings)
        {
            var targetFramework = settings.TargetFramework ?? "netcoreapp3.1";

            return await _analysisHandler.AnalyzeFileIncremental(filePath, projectFile, solutionFilePath,
                preportReferences, currentReferences, rules, externalReferences, settings.ActionsOnly, settings.CompatibleOnly, targetFramework);
        }
        public async Task<IncrementalFileAnalysisResult> AnalyzeFileAsync(string filePath, string fileContent, string projectFile, string solutionFilePath,
            List<string> preportReferences, List<string> currentReferences, RootNodes rules, ExternalReferences externalReferences, AnalyzerSettings settings)
        {
            var targetFramework = settings.TargetFramework ?? "netcoreapp3.1";

            return await _analysisHandler.AnalyzeFileIncremental(filePath, fileContent, projectFile, solutionFilePath,
                preportReferences, currentReferences, rules, externalReferences, settings.ActionsOnly, settings.CompatibleOnly, targetFramework);
        }



        public List<PortingResult> ApplyPortingChanges(PortingRequest request)
        {
            try
            {
                var upgradeVersions = request.RecommendedActions
                    .Where(r => r.RecommendedActionType == RecommendedActionType.UpgradePackage)
                    .Select(recommendation =>
                    {
                        var packageRecommendation = (PackageRecommendation)recommendation;
                        return new Tuple<string, Tuple<string, string>>(packageRecommendation.PackageId, new Tuple<string, string>(packageRecommendation.Version, packageRecommendation.TargetVersions.First()));
                    })
                    .ToDictionary(t => t.Item1, t => t.Item2);

                return _portingHandler.ApplyPortProjectFileChanges(
                    request.Projects,
                    request.SolutionPath,
                    request.TargetFramework,
                    request.IncludeCodeFix,
                    upgradeVersions);
            }
            catch (Exception ex)
            {
                throw new PortingAssistantException("Could not apply porting changes", ex);
            }

        }
    }
}
