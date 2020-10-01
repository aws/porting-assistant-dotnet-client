using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwsCodeAnalyzer;
using AwsCodeAnalyzer.Model;
using PortingAssistant.Analysis.Utils;
using PortingAssistant.Model;
using Microsoft.Extensions.Logging;
using Serilog;
using PortingAssistant.NuGet;
using AnalyzerConfiguration = AwsCodeAnalyzer.AnalyzerConfiguration;

namespace PortingAssistant.Analysis
{
    public class PortingAssistantAnalysisHandler : IPortingAssistantAnalysisHandler
    {
        private readonly ILogger<PortingAssistantAnalysisHandler> _logger;
        private readonly IPortingAssistantNuGetHandler _handler;
        private readonly IPortingAssistantRecommendationHandler _recommendationHandler;
        private static readonly int _maxBuildConcurrency = 1;
        private static readonly SemaphoreSlim _buildConcurrency = new SemaphoreSlim(_maxBuildConcurrency);

        public PortingAssistantAnalysisHandler(ILogger<PortingAssistantAnalysisHandler> logger,
            IPortingAssistantNuGetHandler handler, IPortingAssistantRecommendationHandler recommendationHandler)
        {
            _logger = logger;
            _handler = handler;
            _recommendationHandler = recommendationHandler;
        }

        public Dictionary<string, Task<ProjectAnalysisResult>> AnalyzeSolution(
            string solutionFilename, List<ProjectDetails> project)
        {
            var configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
            {
                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true,
                    ReferenceData = true
                }
            };
            var analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, Log.Logger);
            var analyzersTask = analyzer.AnalyzeSolution(solutionFilename);

            return project
                    .Select((project) => AnalyzeProject(solutionFilename, project, analyzersTask))
                    .Where(p => p.Value != null)
                    .ToDictionary(p => p.Key, p => p.Value);
        }

        private KeyValuePair<string, Task<ProjectAnalysisResult>> AnalyzeProject(
            string solutionFilename, ProjectDetails project, Task<List<AnalyzerResult>> analyzersTask)
        {
            var task = AnalyzeProjectAsync(solutionFilename, project, analyzersTask);
            return KeyValuePair.Create(project.ProjectFilePath, task);
        }

        private async Task<ProjectAnalysisResult> AnalyzeProjectAsync(
            string solutionFilename, ProjectDetails project, Task<List<AnalyzerResult>> analyzersTask)
        {
            try
            {
                _buildConcurrency.Wait();
                var analyzers = await analyzersTask;
                var invocationsMethodSignatures = new HashSet<string>();

                var analyzer = analyzers.Find((a) => a.ProjectResult?.ProjectFilePath != null &&
                    a.ProjectResult.ProjectFilePath.Equals(project.ProjectFilePath));

                if (analyzer == null || analyzer.ProjectResult == null)
                {
                    _logger.LogError("Unable to build {0}.", project.ProjectName);
                    throw new PortingAssistantClientException($"Build {project.ProjectName} failed", null);
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

                var nugetPackages = analyzer.ProjectResult.ExternalReferences.NugetReferences
                    .Select(r => InvocationExpressionModelToInvocations.ReferenceToPackageVersionPair(r))
                    .ToHashSet();

                var subDependencies = analyzer.ProjectResult.ExternalReferences.NugetDependencies
                    .Select(r => InvocationExpressionModelToInvocations.ReferenceToPackageVersionPair(r))
                    .ToHashSet();

                var sdkPackages = analyzer.ProjectResult.ExternalReferences.SdkReferences
                    .Select(r => InvocationExpressionModelToInvocations.ReferenceToPackageVersionPair(r, PackageSourceType.SDK))
                    .ToHashSet();

                var allPackages = nugetPackages
                    .Union(subDependencies)
                    .Union(sdkPackages)
                    .ToList();

                var packageResults = _handler.GetNugetPackages(allPackages, null);
                var recommendationResults = _recommendationHandler.GetApiRecommendation(namespaces.ToList());

                var SourceFileAnalysisResults = InvocationExpressionModelToInvocations.AnalyzeResults(
                    sourceFileToCodeEntityDetails, packageResults, recommendationResults);

                var packageAnalysisResults = nugetPackages.Select(package =>
                {
                    var result = PackageCompatibility.isCompatibleAsync(packageResults.GetValueOrDefault(package, null), package, _logger);
                    var packageAnalysisResult = PackageCompatibility.GetPackageAnalysisResult(result, package);
                    return new Tuple<PackageVersionPair, Task<PackageAnalysisResult>>(package, packageAnalysisResult);
                }).ToDictionary(t => t.Item1, t => t.Item2);

                return new ProjectAnalysisResult
                {
                    ProjectName = project.ProjectName,
                    ProjectFile = project.ProjectFilePath,
                    TargetFrameworks = new List<string> { analyzer.ProjectResult.TargetFramework },
                    PackageReferences = nugetPackages.ToList(),
                    ProjectReferences = analyzer.ProjectResult.ExternalReferences.ProjectReferences.Select(p => p.AssemblyLocation).ToList(),
                    PackageAnalysisResults = packageAnalysisResults,
                    IsBuildFailed = analyzer.ProjectResult.IsBuildFailed(),
                    Errors = analyzer.ProjectBuildResult.BuildErrors,
                    SourceFileAnalysisResults = SourceFileAnalysisResults
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while analyzing {0}, {1}", project.ProjectName, ex);
                throw new PortingAssistantException($"Error while analyzing {project.ProjectName}", ex);
            }
            finally
            {
                _buildConcurrency.Release();
            }
        }
    }
}
