using Codelyzer.Analysis;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using CTA.Rules.Models;
using CTA.Rules.PortCore;
using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Analysis.Utils;
using PortingAssistant.Client.Common.Model;
using PortingAssistant.Client.Common.Utils;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnalyzerConfiguration = Codelyzer.Analysis.AnalyzerConfiguration;
using IDEProjectResult = Codelyzer.Analysis.Build.IDEProjectResult;

namespace PortingAssistant.Client.Analysis
{
    public class PortingAssistantAnalysisHandler : IPortingAssistantAnalysisHandler
    {
        private readonly ILogger<PortingAssistantAnalysisHandler> _logger;
        private readonly IPortingAssistantNuGetHandler _handler;
        private readonly IPortingAssistantRecommendationHandler _recommendationHandler;

        private const string DEFAULT_TARGET = "netcoreapp3.1";

        public PortingAssistantAnalysisHandler(ILogger<PortingAssistantAnalysisHandler> logger,
            IPortingAssistantNuGetHandler handler, IPortingAssistantRecommendationHandler recommendationHandler)
        {
            _logger = logger;
            _handler = handler;
            _recommendationHandler = recommendationHandler;
        }

        public async Task<List<SourceFileAnalysisResult>> AnalyzeFileIncremental(string filePath, string fileContent, string projectFile, string solutionFilePath, List<string> preportReferences
            , List<string> currentReferences, RootNodes projectRules, ExternalReferences externalReferences, bool actionsOnly = false, bool compatibleOnly = false, string targetFramework = DEFAULT_TARGET)
        {
            try
            {
                List<SourceFileAnalysisResult> sourceFileAnalysisResults = new List<SourceFileAnalysisResult>();

                var fileAnalysis = await AnalyzeProjectFiles(projectFile, fileContent, filePath, preportReferences, currentReferences);
                var fileActions = AnalyzeFileActionsIncremental(projectFile, projectRules, targetFramework, solutionFilePath, filePath, fileAnalysis);

                var sourceFileResult = fileAnalysis.RootNodes.FirstOrDefault();

                Dictionary<string, List<CodeEntityDetails>> sourceFileToCodeEntityDetails = new Dictionary<string, List<CodeEntityDetails>>();
                Dictionary<string, Task<RecommendationDetails>> recommendationResults = new Dictionary<string, Task<RecommendationDetails>>();
                Dictionary<PackageVersionPair, Task<PackageDetails>> packageResults = new Dictionary<PackageVersionPair, Task<PackageDetails>>();

                if (!actionsOnly)
                {
                    var sourceFileToInvocations = new[] { SourceFileToCodeTokens(sourceFileResult) }.ToDictionary(result => result.Key, result => result.Value);

                    sourceFileToCodeEntityDetails = CodeEntityModelToCodeEntities.Convert(sourceFileToInvocations, externalReferences);

                    var namespaces = sourceFileToCodeEntityDetails.Aggregate(new HashSet<string>(), (agg, cur) =>
                    {
                        agg.UnionWith(cur.Value.Select(i => i.Namespace).Where(i => i != null));
                        return agg;
                    });

                    var nugetPackages = externalReferences?.NugetReferences?
                            .Select(r => CodeEntityModelToCodeEntities.ReferenceToPackageVersionPair(r))?
                            .ToHashSet();

                    var subDependencies = externalReferences?.NugetDependencies?
                        .Select(r => CodeEntityModelToCodeEntities.ReferenceToPackageVersionPair(r))
                        .ToHashSet();

                    var sdkPackages = namespaces.Select(n => new PackageVersionPair { PackageId = n, Version = "0.0.0", PackageSourceType = PackageSourceType.SDK });

                    var allPackages = nugetPackages
                        .Union(subDependencies)
                        .Union(sdkPackages)
                        .ToList();

                    packageResults = _handler.GetNugetPackages(allPackages, solutionFilePath, isIncremental: true, incrementalRefresh: false);
                    recommendationResults = _recommendationHandler.GetApiRecommendation(namespaces.ToList());
                }

                var portingActionResults = new Dictionary<string, List<RecommendedAction>>();

                var recommendedActions = fileActions.Select(f => new RecommendedAction()
                {
                    Description = f.Description,
                    RecommendedActionType = RecommendedActionType.ReplaceApi,
                    TextSpan = new Model.TextSpan()
                    {
                        StartCharPosition = f.TextSpan.StartCharPosition,
                        EndCharPosition = f.TextSpan.EndCharPosition,
                        StartLinePosition = f.TextSpan.StartLinePosition,
                        EndLinePosition = f.TextSpan.EndLinePosition
                    },
                    TextChanges = f.TextChanges
                }).ToHashSet().ToList();

                portingActionResults.Add(filePath, recommendedActions);

                var sourceFileAnalysisResult = CodeEntityModelToCodeEntities.AnalyzeResults(
                    sourceFileToCodeEntityDetails, packageResults, recommendationResults, portingActionResults, targetFramework, compatibleOnly);

                //In case actions only, result will be empty, so we populate with actions
                if (actionsOnly)
                {
                    sourceFileAnalysisResult.Add(new SourceFileAnalysisResult()
                    {
                        SourceFileName = Path.GetFileName(filePath),
                        SourceFilePath = filePath,
                        RecommendedActions = recommendedActions,
                        ApiAnalysisResults = new List<ApiAnalysisResult>()
                    });
                }

                sourceFileAnalysisResults.AddRange(sourceFileAnalysisResult);


                return sourceFileAnalysisResults;
            }
            finally
            {
                CommonUtils.RunGarbageCollection(_logger, "PortingAssistantAnalysisHandler.AnalyzeFileIncremental");
            }
        }

        public async Task<List<SourceFileAnalysisResult>> AnalyzeFileIncremental(string filePath, string projectFile, string solutionFilePath, List<string> preportReferences
            , List<string> currentReferences, RootNodes projectRules, ExternalReferences externalReferences, bool actionsOnly, bool compatibleOnly, string targetFramework = DEFAULT_TARGET)
        {
            var fileContent = File.ReadAllText(filePath);
            return await AnalyzeFileIncremental(filePath, fileContent, projectFile, solutionFilePath, preportReferences, currentReferences, projectRules, externalReferences, actionsOnly, compatibleOnly, targetFramework);
        }

        public async Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolutionIncremental(string solutionFilename, List<string> projects,
            string targetFramework = DEFAULT_TARGET)
        {
            try
            {
                MemoryUtils.LogSolutiontSize(_logger, solutionFilename);
                var configuration = GetAnalyzerConfiguration();
                var analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, _logger);
                var analyzersTask = await analyzer.AnalyzeSolution(solutionFilename);                

                var analysisActions = AnalyzeActions(projects, targetFramework, analyzersTask, solutionFilename);

                var analyzerResult = analyzersTask;

                var solutionAnalysisResult = projects
                        .Select((project) => new KeyValuePair<string, ProjectAnalysisResult>(project, AnalyzeProject(project, solutionFilename, analyzersTask, analysisActions, isIncremental: true, targetFramework)))
                        .Where(p => p.Value != null)
                        .ToDictionary(p => p.Key, p => p.Value);


                var projectActions = projects
                       .Select((project) => new KeyValuePair<string, ProjectActions>
                       (project, analysisActions.FirstOrDefault(p => p.ProjectFile == project)?.ProjectActions ?? new ProjectActions()))
                       .Where(p => p.Value != null)
                       .ToDictionary(p => p.Key, p => p.Value);


                return solutionAnalysisResult;
            }
            catch (OutOfMemoryException e)
            {
                _logger.LogError("Analyze solution {0} with error {1}", solutionFilename, e);
                MemoryUtils.LogMemoryConsumption(_logger);
                throw e;
            }
            finally
            {
                CommonUtils.RunGarbageCollection(_logger, "PortingAssistantAnalysisHandler.AnalyzeSolutionIncremental");
            }
        }

        private List<IDEFileActions> AnalyzeFileActionsIncremental(string project, RootNodes rootNodes, string targetFramework
            , string pathToSolution, string filePath, IDEProjectResult projectResult)
        {
            List<PortCoreConfiguration> configs = new List<PortCoreConfiguration>();

            PortCoreConfiguration projectConfiguration = new PortCoreConfiguration()
            {
                ProjectPath = project,
                UseDefaultRules = true,
                TargetVersions = new List<string> { targetFramework },
                PortCode = false,
                PortProject = false
            };

            projectResult.ProjectPath = project;

            configs.Add(projectConfiguration);

            var solutionPort = new SolutionPort(pathToSolution, projectResult, configs);
            return solutionPort.RunIncremental(rootNodes, filePath);
        }

        private async Task<IDEProjectResult> AnalyzeProjectFiles(string projectPath, string fileContent, string filePath, List<string> preportReferences, List<string> currentReferences)
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
                var ideProjectResult = await analyzer.AnalyzeFile(projectPath, filePath, fileContent, preportReferences, currentReferences);

                return ideProjectResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while analyzing files");
            }
            finally
            {
                CommonUtils.RunGarbageCollection(_logger, "PortingAssistantAnalysisHandler.AnalyzeFileIncremental");
            }
            return null;
        }

        public async Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolution(
            string solutionFilename, List<string> projects, string targetFramework = DEFAULT_TARGET)
        {
            try
            {
                MemoryUtils.LogSolutiontSize(_logger, solutionFilename);
                var configuration = GetAnalyzerConfiguration();
                var analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, _logger);
                var analyzersTask = await analyzer.AnalyzeSolution(solutionFilename);

                var analysisActions = AnalyzeActions(projects, targetFramework, analyzersTask, solutionFilename);

                return projects
                        .Select((project) => new KeyValuePair<string, ProjectAnalysisResult>(project, AnalyzeProject(project, solutionFilename, analyzersTask, analysisActions, isIncremental: false, targetFramework)))
                        .Where(p => p.Value != null)
                        .ToDictionary(p => p.Key, p => p.Value);
            }
            catch (OutOfMemoryException e)
            {
                _logger.LogError("Analyze solution {0} with error {1}", solutionFilename, e);
                MemoryUtils.LogMemoryConsumption(_logger);
                throw e;
            }
            catch(Exception ex)
            {
                throw ex;
            }
            finally
            {
                CommonUtils.RunGarbageCollection(_logger, "PortingAssistantAnalysisHandler.AnalyzeSolution");
            }

        }


        public async Task<Dictionary<string, ProjectAnalysisResult>> AnalyzeSolutionGenerator(
            string solutionFilename, List<string> projects, string targetFramework = DEFAULT_TARGET)
        {
            try
            {
                var configuration = GetAnalyzerConfiguration();
                var analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, _logger);

                var resultEnumerator = analyzer.AnalyzeSolutionGeneratorAsync(solutionFilename).GetAsyncEnumerator();
                SolutionPort solutionPort = new SolutionPort(solutionFilename);
                var resultsDictionary = new Dictionary<string, ProjectAnalysisResult>();

                //Init
                while (await resultEnumerator.MoveNextAsync())
                {
                    using var result = resultEnumerator.Current;
                    var projectPath = result?.ProjectResult?.ProjectFilePath;
                    PortCoreConfiguration projectConfiguration = new PortCoreConfiguration()
                    {
                        ProjectPath = projectPath,
                        UseDefaultRules = true,
                        TargetVersions = new List<string> { targetFramework },
                        PortCode = false,
                        PortProject = false
                    };

                    var projectResult = solutionPort.RunProject(result, projectConfiguration);

                    var analysisResult = AnalyzeProject(projectPath, solutionFilename, result, new ProjectActions(), isIncremental: false, targetFramework);

                    projectResult = null;
                    analysisResult = null;
                    resultsDictionary.Add(projectPath, analysisResult);
                }                
                //Terminate
                return resultsDictionary;
            }
            catch (OutOfMemoryException e)
            {
                _logger.LogError("Analyze solution {0} with error {1}", solutionFilename, e);
                MemoryUtils.LogMemoryConsumption(_logger);
                throw e;
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
                    PortCode = false,
                    PortProject = false
                };

                configs.Add(projectConfiguration);
            }
            var solutionPort = new SolutionPort(pathToSolution, analyzerResults, configs, _logger);
            return solutionPort.Run().ProjectResults.ToList();
        }

        private KeyValuePair<string, UstList<UstNode>> SourceFileToCodeTokens(RootUstNode sourceFile)
        {
            var allNodes = new UstList<UstNode>();
            allNodes.AddRange(sourceFile.AllInvocationExpressions());
            allNodes.AddRange(sourceFile.AllAnnotations());
            allNodes.AddRange(sourceFile.AllDeclarationNodes());
            allNodes.AddRange(sourceFile.AllStructDeclarations());
            allNodes.AddRange(sourceFile.AllEnumDeclarations());

            return KeyValuePair.Create(sourceFile.FileFullPath, allNodes);
        }

        private ProjectAnalysisResult AnalyzeProject(
            string project, string solutionFileName, List<AnalyzerResult> analyzers, List<ProjectResult> analysisActions, bool isIncremental = false, string targetFramework = DEFAULT_TARGET)
        {
            try
            {
                var analyzer = analyzers.Find((a) => a.ProjectResult?.ProjectFilePath != null &&
                    a.ProjectResult.ProjectFilePath.Equals(project));
                var projectActions = analysisActions.FirstOrDefault(p => p.ProjectFile == project)?.ProjectActions ?? new ProjectActions();

                return AnalyzeProject(project, solutionFileName, analyzer, projectActions, isIncremental, targetFramework);
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

        private ProjectAnalysisResult AnalyzeProject(
           string project, string solutionFileName, AnalyzerResult analyzer, ProjectActions projectActions, bool isIncremental = false, string targetFramework = DEFAULT_TARGET)
        {
            try
            {
                if (analyzer == null || analyzer.ProjectResult == null)
                {
                    _logger.LogError("Unable to build {0}.", project);
                    return null;
                }

                var sourceFileToCodeTokens = analyzer.ProjectResult.SourceFileResults.Select((sourceFile) =>
                {
                    return SourceFileToCodeTokens(sourceFile);
                }).ToDictionary(p => p.Key, p => p.Value);

                var sourceFileToCodeEntityDetails = CodeEntityModelToCodeEntities.Convert(sourceFileToCodeTokens, analyzer);

                var namespaces = sourceFileToCodeEntityDetails.Aggregate(new HashSet<string>(), (agg, cur) =>
                {
                    agg.UnionWith(cur.Value.Select(i => i.Namespace).Where(i => i != null));
                    return agg;
                });

                var targetframeworks = analyzer.ProjectResult.TargetFrameworks.Count == 0 ?
                    new List<string> { analyzer.ProjectResult.TargetFramework } : analyzer.ProjectResult.TargetFrameworks;

                var nugetPackages = analyzer.ProjectResult.ExternalReferences.NugetReferences
                    .Select(r => CodeEntityModelToCodeEntities.ReferenceToPackageVersionPair(r))
                    .ToHashSet();

                var subDependencies = analyzer.ProjectResult.ExternalReferences.NugetDependencies
                    .Select(r => CodeEntityModelToCodeEntities.ReferenceToPackageVersionPair(r))
                    .ToHashSet();

                var sdkPackages = namespaces.Select(n => new PackageVersionPair { PackageId = n, Version = "0.0.0", PackageSourceType = PackageSourceType.SDK });

                var allPackages = nugetPackages
                    .Union(subDependencies)
                    .Union(sdkPackages)
                    .ToList();

                Dictionary<PackageVersionPair, Task<PackageDetails>> packageResults;

                if (isIncremental)
                    packageResults = _handler.GetNugetPackages(allPackages, solutionFileName, isIncremental: true, incrementalRefresh: true);
                else
                    packageResults = _handler.GetNugetPackages(allPackages, null, isIncremental: false, incrementalRefresh: false);

                var recommendationResults = _recommendationHandler.GetApiRecommendation(namespaces.ToList());

                var packageAnalysisResults = nugetPackages.Select(package =>
                {
                    var result = PackageCompatibility.IsCompatibleAsync(packageResults.GetValueOrDefault(package, null), package, _logger, targetFramework);
                    var packageAnalysisResult = PackageCompatibility.GetPackageAnalysisResult(result, package, targetFramework);
                    return new Tuple<PackageVersionPair, Task<PackageAnalysisResult>>(package, packageAnalysisResult);
                }).ToDictionary(t => t.Item1, t => t.Item2);

                var portingActionResults = ProjectActionsToRecommendedActions.Convert(projectActions);

                var SourceFileAnalysisResults = CodeEntityModelToCodeEntities.AnalyzeResults(
                    sourceFileToCodeEntityDetails, packageResults, recommendationResults, portingActionResults, targetFramework);

                var compatibilityResults = GenerateCompatibilityResults(SourceFileAnalysisResults, analyzer.ProjectResult.ProjectFilePath, analyzer.ProjectBuildResult?.PrePortCompilation != null);

                return new ProjectAnalysisResult
                {
                    ProjectName = analyzer.ProjectResult.ProjectName,
                    ProjectFilePath = analyzer.ProjectResult.ProjectFilePath,
                    TargetFrameworks = targetframeworks,
                    PackageReferences = nugetPackages.ToList(),
                    ProjectReferences = analyzer.ProjectResult.ExternalReferences.ProjectReferences.ConvertAll(p => new ProjectReference { ReferencePath = p.AssemblyLocation }),
                    PackageAnalysisResults = packageAnalysisResults,
                    IsBuildFailed = analyzer.ProjectResult.IsBuildFailed() || analyzer.ProjectBuildResult.IsSyntaxAnalysis,
                    Errors = analyzer.ProjectResult.BuildErrors,
                    ProjectGuid = analyzer.ProjectResult.ProjectGuid,
                    ProjectType = analyzer.ProjectResult.ProjectType,
                    SourceFileAnalysisResults = SourceFileAnalysisResults,
                    MetaReferences = analyzer.ProjectBuildResult.Project.MetadataReferences.Select(m => m.Display).ToList(),
                    PreportMetaReferences = analyzer.ProjectBuildResult.PreportReferences,
                    ProjectRules = projectActions.ProjectRules,
                    ExternalReferences = analyzer.ProjectResult.ExternalReferences,
                    ProjectCompatibilityResult = compatibilityResults
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


        private ProjectCompatibilityResult GenerateCompatibilityResults(List<SourceFileAnalysisResult> sourceFileAnalysisResults, string projectPath, bool isPorted)
        {
            var projectCompatibilityResult = new ProjectCompatibilityResult() { IsPorted = isPorted, ProjectPath = projectPath };

            sourceFileAnalysisResults.ForEach(SourceFileAnalysisResult => {
                SourceFileAnalysisResult.ApiAnalysisResults.ForEach(apiAnalysisResult =>
                {
                    var currentEntity = projectCompatibilityResult.CodeEntityCompatibilityResults.First(r => r.CodeEntityType == apiAnalysisResult.CodeEntityDetails.CodeEntityType);

                    var hasAction = SourceFileAnalysisResult.RecommendedActions.Any(ra => ra.TextSpan.Equals(apiAnalysisResult.CodeEntityDetails.TextSpan));
                    if (hasAction)
                    {
                        currentEntity.Actions++;
                    }
                    var compatibility = apiAnalysisResult.CompatibilityResults?.FirstOrDefault().Value?.Compatibility;
                    if (compatibility == Compatibility.COMPATIBLE)
                    {
                        currentEntity.Compatible++;
                    }
                    else if (compatibility == Compatibility.INCOMPATIBLE)
                    {
                        currentEntity.Incompatible++;
                    }
                    else if (compatibility == Compatibility.UNKNOWN)
                    {
                        currentEntity.Unknown++;

                    }
                    else if (compatibility == Compatibility.DEPRECATED)
                    {
                        currentEntity.Deprecated++;
                    }
                    else
                    {
                        currentEntity.Unknown++;
                    }
                });
            });

            _logger.LogInformation($"{projectCompatibilityResult.ToString()}");
            return projectCompatibilityResult;
        }

        private AnalyzerConfiguration GetAnalyzerConfiguration()
        {
            return new AnalyzerConfiguration(LanguageOptions.CSharp)
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
                    },
                ConcurrentThreads = 1
            };
        }
    }
}
