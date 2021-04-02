using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTA.Rules.Models;
using CTA.Rules.PortCore;
using Codelyzer.Analysis;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Analysis.Utils;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.NuGet;
using AnalyzerConfiguration = Codelyzer.Analysis.AnalyzerConfiguration;

namespace PortingAssistant.Client.Analysis
{
    public class PortingAssistantAnalysisHandler : IPortingAssistantAnalysisHandler
    {
        private readonly ILogger<PortingAssistantAnalysisHandler> _logger;
        private readonly IPortingAssistantNuGetHandler _handler;
        private readonly IPortingAssistantRecommendationHandler _recommendationHandler;

        public PortingAssistantAnalysisHandler(ILogger<PortingAssistantAnalysisHandler> logger,
            IPortingAssistantNuGetHandler handler, IPortingAssistantRecommendationHandler recommendationHandler)
        {
            _logger = logger;
            _handler = handler;
            _recommendationHandler = recommendationHandler;
        }

        public async Task<IncrementalFileAnalysisResult> AnalyzeFileIncremental(string filePath, string project, string solutionFileName, List<AnalyzerResult> existingAnalyzerResults
            , Dictionary<string, ProjectActions> existingProjectActions, string targetFramework = "netcoreapp3.1")
        {
            if (existingAnalyzerResults == null)
            {
                _logger.LogDebug("Existing Codelyzer result is null.");
                return null;
            }
            var analyzerResults = await GetUpdatedAnalyzer(filePath, existingAnalyzerResults);

            var analyzerResult = analyzerResults.First(analyzerResults => analyzerResults.ProjectBuildResult.SourceFileBuildResults.Any(s => s.SourceFileFullPath == filePath));
       
            var sourceFileResult = analyzerResult.ProjectResult.SourceFileResults.FirstOrDefault(sourceFile => sourceFile.FileFullPath == filePath);

            var sourceFileToInvocations = new[] { KeyValuePair.Create(sourceFileResult.FileFullPath, sourceFileResult.AllInvocationExpressions()) }
            .ToDictionary(p => p.Key, p => p.Value);

            var sourceFileToCodeEntityDetails = InvocationExpressionModelToInvocations.Convert(sourceFileToInvocations, analyzerResult);

            var namespaces = sourceFileToCodeEntityDetails.Aggregate(new HashSet<string>(), (agg, cur) =>
            {
                agg.UnionWith(cur.Value.Select(i => i.Namespace).Where(i => i != null));
                return agg;
            });

            var nugetPackages = analyzerResult.ProjectResult.ExternalReferences.NugetReferences
                    .Select(r => InvocationExpressionModelToInvocations.ReferenceToPackageVersionPair(r))
                    .ToHashSet();

            var subDependencies = analyzerResult.ProjectResult.ExternalReferences.NugetDependencies
                .Select(r => InvocationExpressionModelToInvocations.ReferenceToPackageVersionPair(r))
                .ToHashSet();

            var sdkPackages = namespaces.Select(n => new PackageVersionPair { PackageId = n, Version = "0.0.0", PackageSourceType = PackageSourceType.SDK });

            var allPackages = nugetPackages
                .Union(subDependencies)
                .Union(sdkPackages)
                .ToList();

            var packageResults = _handler.GetAndCacheNugetPackages(allPackages, solutionFileName);
            var recommendationResults = _recommendationHandler.GetApiRecommendation(namespaces.ToList());

            var analysisActionsForFile = AnalyzeFileActions(project, analyzerResults, existingProjectActions, targetFramework, solutionFileName, sourceFileResult.FileFullPath);
            var projectActionsForFile = analysisActionsForFile.FirstOrDefault(p => p.ProjectFile == project)?.ProjectActions ?? new ProjectActions();

            var portingActionResults = ProjectActionsToRecommendedActions.Convert(projectActionsForFile);
            
            var sourceFileAnalysisResults = InvocationExpressionModelToInvocations.AnalyzeResults(
                sourceFileToCodeEntityDetails, packageResults, recommendationResults, portingActionResults, targetFramework);

            return new IncrementalFileAnalysisResult()
            {
                sourceFileAnalysisResults = sourceFileAnalysisResults,
                projectActions = projectActionsForFile,
                analyzerResults = analyzerResults
            };
        }

        public async Task<IncrementalProjectAnalysisResultDict> AnalyzeSolutionIncremental(string solutionFilename, List<string> projects, 
            string targetFramework = "netcoreapp3.1")
        {
            try
            {
                var configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
                {
                    MetaDataSettings =
                    {
                        LiteralExpressions = true,
                        MethodInvocations = true,
                        ReferenceData = true,
                        Annotations = true,
                        DeclarationNodes = true,
                        LoadBuildData = true,
                        LocationData = true,
                        InterfaceDeclarations = true
                    }
                };
                var analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, _logger);
                var analyzersTask = await analyzer.AnalyzeSolution(solutionFilename);

                var analysisActions = AnalyzeActions(projects, targetFramework, analyzersTask, solutionFilename);

                var analyzerResult = analyzersTask;

                var solutionAnalysisResult = projects
                        .Select((project) => new KeyValuePair<string, ProjectAnalysisResult>(project, AnalyzeProject(project, analyzersTask, analysisActions, isIncremental: true, targetFramework)))
                        .Where(p => p.Value != null)
                        .ToDictionary(p => p.Key, p => p.Value);

                var projectActions = projects
                       .Select((project) => new KeyValuePair<string, ProjectActions>
                       (project, analysisActions.FirstOrDefault(p => p.ProjectFile == project)?.ProjectActions ?? new ProjectActions()))
                       .Where(p => p.Value != null)
                       .ToDictionary(p => p.Key, p => p.Value);

                return new IncrementalProjectAnalysisResultDict()
                {
                    projectAnalysisResultDict = solutionAnalysisResult,
                    projectActions = projectActions,
                    analyzerResults = analyzerResult
                };
            }
            finally
            {
                CommonUtils.RunGarbageCollection(_logger, "PortingAssistantAnalysisHandler.AnalyzeSolution");
            }
        }

        private List<ProjectResult> AnalyzeFileActions(string project, List<AnalyzerResult> analyzerResults, 
            Dictionary<string, ProjectActions> existingProjectActions, string targetFramework, string pathToSolution, string filePath)
        {
            List<PortCoreConfiguration> configs = new List<PortCoreConfiguration>();
            List<string> updatedFiles = new List<string>();

            PortCoreConfiguration projectConfiguration = new PortCoreConfiguration()
            {
                ProjectPath = project,
                UseDefaultRules = true,
                TargetVersions = new List<string> { targetFramework },
            };

            configs.Add(projectConfiguration);
            updatedFiles.Add(filePath);

            var solutionPort = new SolutionPort(pathToSolution, analyzerResults, configs, _logger);

            // Return sourceFileToInvocations for the file changed.
            return solutionPort.RunIncremental(existingProjectActions, updatedFiles).ProjectResults.ToList();
        }

        public async Task<List<AnalyzerResult>> GetUpdatedAnalyzer(string filePath, List<AnalyzerResult> currAnalyzerResult)
        {
            List<AnalyzerResult> updatedAnalyzerResult;
            
            try
            {
                var configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
                {
                    MetaDataSettings =
                    {
                        LiteralExpressions = true,
                        MethodInvocations = true,
                        ReferenceData = true,
                        Annotations = true,
                        DeclarationNodes = true,
                        LoadBuildData = true,
                        LocationData = true,
                        InterfaceDeclarations = true
                    }
                };
                var analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, _logger);
                updatedAnalyzerResult = await analyzer.AnalyzeFile(filePath, currAnalyzerResult);
                return updatedAnalyzerResult;
            }
            finally
            {
                CommonUtils.RunGarbageCollection(_logger, "PortingAssistantAnalysisHandler.AnalyzeFileIncremental");
            }
        }

        public async Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolution(
            string solutionFilename, List<string> projects, string targetFramework = "netcoreapp3.1")
        {
            try
            {
                var configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
                {
                    MetaDataSettings =
                    {
                        LiteralExpressions = true,
                        MethodInvocations = true,
                        ReferenceData = true,
                        Annotations = true,
                        DeclarationNodes = true,
                        LoadBuildData = true,
                        LocationData = true,
                        InterfaceDeclarations = true
                    }
                };
                var analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, _logger);
                var analyzersTask = await analyzer.AnalyzeSolution(solutionFilename);

                var analysisActions = AnalyzeActions(projects, targetFramework, analyzersTask, solutionFilename);

                return projects
                        .Select((project) => new KeyValuePair<string, ProjectAnalysisResult>(project, AnalyzeProject(project, analyzersTask, analysisActions, isIncremental: false, targetFramework)))
                        .Where(p => p.Value != null)
                        .ToDictionary(p => p.Key, p => p.Value);
            }
            finally
            {
                CommonUtils.RunGarbageCollection(_logger, "PortingAssistantAnalysisHandler.AnalyzeSolution");
            }

        }

        private List<ProjectResult> AnalyzeActions(List<string> projects, string targetFramework, List<AnalyzerResult> analyzerResults, string pathToSolution)
        {
            List<PortCoreConfiguration> configs = new List<PortCoreConfiguration>();

            var anaylyzedProjects = projects.Where(p =>
            {
                var project = analyzerResults.Find((a) => a.ProjectResult?.ProjectFilePath != null &&
                    a.ProjectResult.ProjectFilePath.Equals(p));
                return project != null;
            }).ToList();

            foreach (var proj in anaylyzedProjects)
            {
                PortCoreConfiguration projectConfiguration = new PortCoreConfiguration()
                {
                    ProjectPath = proj,
                    UseDefaultRules = true,
                    TargetVersions = new List<string> { targetFramework },
                };

                configs.Add(projectConfiguration);
            }
            var solutionPort = new SolutionPort(pathToSolution, analyzerResults, configs, _logger);
            return solutionPort.AnalysisRun().ProjectResults.ToList();
        }

        private ProjectAnalysisResult AnalyzeProject(
            string project, List<AnalyzerResult> analyzers, List<ProjectResult> analysisActions, bool isIncremental, string targetFramework = "netcoreapp3.1")
        {
            try
            {
                var analyzer = analyzers.Find((a) => a.ProjectResult?.ProjectFilePath != null &&
                    a.ProjectResult.ProjectFilePath.Equals(project));

                var projectActions = analysisActions.FirstOrDefault(p => p.ProjectFile == project)?.ProjectActions ?? new ProjectActions();

                if (analyzer == null || analyzer.ProjectResult == null)
                {
                    _logger.LogError("Unable to build {0}.", project);
                    return null;
                }

                var sourceFileToInvocations = analyzer.ProjectResult.SourceFileResults.Select((sourceFile) =>
                {
                    var invocationsInSourceFile = sourceFile.AllInvocationExpressions();
                    _logger.LogInformation("API: SourceFile {0} has {1} invocations pre-filter", sourceFile.FileFullPath, invocationsInSourceFile.Count());
                    return KeyValuePair.Create(sourceFile.FileFullPath, invocationsInSourceFile);
                }).ToDictionary(p => p.Key, p => p.Value);

                var sourceFileToCodeEntityDetails = InvocationExpressionModelToInvocations.Convert(sourceFileToInvocations, analyzer);

                var namespaces = sourceFileToCodeEntityDetails.Aggregate(new HashSet<string>(), (agg, cur) =>
                {
                    agg.UnionWith(cur.Value.Select(i => i.Namespace).Where(i => i != null));
                    return agg;
                });

                var targetframeworks = analyzer.ProjectResult.TargetFrameworks.Count == 0 ?
                    new List<string> { analyzer.ProjectResult.TargetFramework } : analyzer.ProjectResult.TargetFrameworks;

                var nugetPackages = analyzer.ProjectResult.ExternalReferences.NugetReferences
                    .Select(r => InvocationExpressionModelToInvocations.ReferenceToPackageVersionPair(r))
                    .ToHashSet();

                var subDependencies = analyzer.ProjectResult.ExternalReferences.NugetDependencies
                    .Select(r => InvocationExpressionModelToInvocations.ReferenceToPackageVersionPair(r))
                    .ToHashSet();

                var sdkPackages = namespaces.Select(n => new PackageVersionPair { PackageId = n, Version = "0.0.0", PackageSourceType = PackageSourceType.SDK });

                var allPackages = nugetPackages
                    .Union(subDependencies)
                    .Union(sdkPackages)
                    .ToList();

                var packageResults = _handler.GetNugetPackages(allPackages, null);
                var recommendationResults = _recommendationHandler.GetApiRecommendation(namespaces.ToList());

                var packageAnalysisResults = nugetPackages.Select(package =>
                {
                    var result = PackageCompatibility.IsCompatibleAsync(packageResults.GetValueOrDefault(package, null), package, _logger, targetFramework);
                    var packageAnalysisResult = PackageCompatibility.GetPackageAnalysisResult(result, package, targetFramework);
                    return new Tuple<PackageVersionPair, Task<PackageAnalysisResult>>(package, packageAnalysisResult);
                }).ToDictionary(t => t.Item1, t => t.Item2);

                var portingActionResults = ProjectActionsToRecommendedActions.Convert(projectActions);                                                                                                                                                                  
                
                var SourceFileAnalysisResults = InvocationExpressionModelToInvocations.AnalyzeResults(
                    sourceFileToCodeEntityDetails, packageResults, recommendationResults, portingActionResults, targetFramework);



                return new ProjectAnalysisResult
                {
                    ProjectName = analyzer.ProjectResult.ProjectName,
                    ProjectFilePath = analyzer.ProjectResult.ProjectFilePath,
                    TargetFrameworks = targetframeworks,
                    PackageReferences = nugetPackages.ToList(),
                    ProjectReferences = analyzer.ProjectResult.ExternalReferences.ProjectReferences.ConvertAll(p => new ProjectReference { ReferencePath = p.AssemblyLocation }),
                    PackageAnalysisResults = packageAnalysisResults,
                    IsBuildFailed = analyzer.ProjectResult.IsBuildFailed(),
                    Errors = analyzer.ProjectResult.BuildErrors,
                    ProjectGuid = analyzer.ProjectResult.ProjectGuid,
                    ProjectType = analyzer.ProjectResult.ProjectType,
                    SourceFileAnalysisResults = SourceFileAnalysisResults
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while analyzing {0}, {1}", project, ex);
                return new ProjectAnalysisResult
                {
                    ProjectName = Path.GetFileNameWithoutExtension(project),
                    ProjectFilePath = project,
                    TargetFrameworks = new List<string>(),
                    PackageReferences = new List<PackageVersionPair>(),
                    ProjectReferences = new List<ProjectReference>(),
                    PackageAnalysisResults = new Dictionary<PackageVersionPair, Task<PackageAnalysisResult>>(),
                    IsBuildFailed = true,
                    Errors = new List<string> { string.Format("Error while analyzing {0}, {1}", project, ex) },
                    ProjectGuid = null,
                    ProjectType = null,
                    SourceFileAnalysisResults = new List<SourceFileAnalysisResult>()
                };
            }
        }
    }
}
