using System;
using System.Collections.Generic;
using System.Linq;
using PortingAssistant.Client.Analysis;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.Porting;
using Microsoft.Build.Construction;
using System.IO;
using System.Threading.Tasks;
using CTA.Rules.Models;
using Codelyzer.Analysis.Model;
using Codelyzer.Analysis;
using System.Threading;
using System.Runtime.CompilerServices;
using PortingAssistant.Compatibility.Common.Model;

namespace PortingAssistant.Client.Client
{
    
    public class PortingAssistantClient : IPortingAssistantClient
    {
        private readonly IPortingAssistantAnalysisHandler _analysisHandler;
        private readonly IPortingHandler _portingHandler;

        private const string DEFAULT_TARGET = "net6.0";

        public PortingAssistantClient(
            IPortingAssistantAnalysisHandler AnalysisHandler,
            IPortingHandler portingHandler)
        {
            _analysisHandler = AnalysisHandler;
            _portingHandler = portingHandler;
        }


        public async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionFilePath, AnalyzerSettings settings,
            AssessmentType assessmentType = AssessmentType.FullAssessment)
        {
            try
            {
                var _ = SolutionFile.Parse(solutionFilePath);
                var failedProjects = new List<string>();

                var projects = ProjectsToAnalyze(solutionFilePath, settings);

                var targetFramework = settings.TargetFramework ?? DEFAULT_TARGET;

                Dictionary<string, ProjectAnalysisResult> projectAnalysisResultsDict;

                if (settings.ContiniousEnabled)
                    projectAnalysisResultsDict = await _analysisHandler.AnalyzeSolutionIncremental(solutionFilePath, projects, targetFramework, settings);
                else
                    projectAnalysisResultsDict = await _analysisHandler.AnalyzeSolution(solutionFilePath, projects, targetFramework, settings, assessmentType);

                var projectAnalysisResults = projects.Select(async p =>
                {
                    projectAnalysisResultsDict = await _analysisHandler.AnalyzeSolution(solutionFilePath, projects, targetFramework);
                });

                return GenerateSolutionAnalysisResult(solutionFilePath, projectAnalysisResultsDict, projects);
            }
            catch (Exception ex)
            {
                throw new PortingAssistantException($"Cannot Analyze solution {solutionFilePath}", ex);
            }
        }

        public async IAsyncEnumerable<ProjectAnalysisResult> AnalyzeSolutionGeneratorAsync(string solutionFilePath, AnalyzerSettings settings, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var targetFramework = settings.TargetFramework ?? DEFAULT_TARGET;

            var projects = ProjectsToAnalyze(solutionFilePath, settings);
            var resultEnumerator = _analysisHandler.AnalyzeSolutionGeneratorAsync(solutionFilePath, projects, targetFramework, cancellationToken).GetAsyncEnumerator();
            try
            {
                while (await resultEnumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = resultEnumerator.Current;
                    yield return result;
                }
            }
            finally
            {
                await resultEnumerator.DisposeAsync();
            }
        }

        public List<PortingResult> ApplyPortingChanges(PortingRequest request)
        {
            try
            {
                var upgradeVersions = request.RecommendedActions
                    .Where(r => r.RecommendedActionType == Model.RecommendedActionType.UpgradePackage)
                    .Select(recommendation =>
                    {
                        var packageRecommendation = (PackageRecommendation)recommendation;
                        return new Tuple<string, Tuple<string, string>>(packageRecommendation.PackageId, new Tuple<string, string>(packageRecommendation.Version, packageRecommendation.TargetVersions.First()));
                    })
                    .GroupBy(t => t.Item1).Select(t => t.FirstOrDefault())
                    .ToDictionary(t => t.Item1, t => t.Item2);

                return _portingHandler.ApplyPortProjectFileChanges(
                    request.Projects,
                    request.SolutionPath,
                    request.TargetFramework,
                    request.IncludeCodeFix,
                    upgradeVersions, request.VisualStudioVersion);
            }
            catch (Exception ex)
            {
                throw new PortingAssistantException("Could not apply porting changes", ex);
            }

        }

        public List<string> ProjectsToAnalyze(string solutionFilePath, AnalyzerSettings settings)
        {
            var solution = SolutionFile.Parse(solutionFilePath);
            return solution.ProjectsInOrder.Where(p =>
                (p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat ||
                p.ProjectType == SolutionProjectType.WebProject) &&
                (settings.IgnoreProjects?.Contains(p.AbsolutePath) != true))
                .Select(p => p.AbsolutePath)
                .ToList();
        }


        public SolutionAnalysisResult GetCompatibilityResults(string solutionFilePath, AnalyzerSettings settings, List<AnalyzerResult> analyzerResults)
        {
            try
            {
                var projects = ProjectsToAnalyze(solutionFilePath, settings);

                var targetFramework = settings.TargetFramework ?? DEFAULT_TARGET;

                Dictionary<string, ProjectAnalysisResult> projectAnalysisResultsDict;

                if (settings.ContiniousEnabled)
                {
                    projectAnalysisResultsDict = _analysisHandler.GetCompatibilityResultsIncremental(solutionFilePath, projects, analyzerResults, targetFramework);
                }
                else
                {
                    projectAnalysisResultsDict = _analysisHandler.GetCompatibilityResults(solutionFilePath, projects, analyzerResults, targetFramework);
                }

                return GenerateSolutionAnalysisResult(solutionFilePath, projectAnalysisResultsDict, projects);
            }
            catch (Exception ex)
            {
                throw new PortingAssistantException($"Cannot Analyze solution {solutionFilePath}", ex);
            }
        }

        public SolutionAnalysisResult GenerateSolutionAnalysisResult(string solutionFilePath, Dictionary<string, ProjectAnalysisResult> projectAnalysisResultsDict, List<string> projects)
        {
            var failedProjects = new List<string>();
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

            var solutionGuid = FileParser.SolutionFileParser.getSolutionGuid(solutionFilePath);
            var solutionDetails = new SolutionDetails
            {
                SolutionName = Path.GetFileNameWithoutExtension(solutionFilePath),
                SolutionFilePath = solutionFilePath,
                SolutionGuid = solutionGuid,
                RepositoryUrl = FileParser.GitConfigFileParser.getGitRepositoryUrl(
                    FileParser.GitConfigFileParser.getGitRepositoryRootPath(solutionFilePath)),
                ApplicationGuid = solutionGuid ?? Utils.HashUtils.GenerateGuid(
                    projectAnalysisResults.Select(p => p.ProjectGuid).ToList()),
                Projects = projectAnalysisResults.ConvertAll(p => new ProjectDetails
                {
                    PackageReferences = p.PackageReferences,
                    ProjectFilePath = p.ProjectFilePath,
                    ProjectGuid = p.ProjectGuid,
                    FeatureType = p.FeatureType,
                    ProjectName = p.ProjectName,
                    ProjectReferences = p.ProjectReferences,
                    ProjectType = p.ProjectType,
                    TargetFrameworks = p.TargetFrameworks,
                    IsBuildFailed = p.IsBuildFailed,
                    LinesOfCode = p.LinesOfCode,
                }),

                FailedProjects = failedProjects
            };

            return new SolutionAnalysisResult
            {
                FailedProjects = failedProjects,
                SolutionDetails = solutionDetails,
                ProjectAnalysisResults = projectAnalysisResults,
                HasCompatibilityCheckingError = projectAnalysisResults.Any(c=>c.HasCompatibilityCheckingError) //if any project has compatibilityCheckerResponse Error
            };
        }
    }
}
