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
using PortingAssistant.Utils;
using AnalyzerConfiguration = AwsCodeAnalyzer.AnalyzerConfiguration;
using System.IO;

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

                var analyzer = analyzers.Find((a) => a.ProjectResult.ProjectFilePath.Equals(project.ProjectFilePath));

                if (analyzer == null || analyzer.ProjectResult == null)
                {
                    _logger.LogError("Unable to build {0}.", project.ProjectName);
                    throw new PortingAssistantClientException($"Build {project.ProjectName} failed", null);
                }

                if (analyzer.ProjectResult.BuildErrorsCount > 0 && analyzer.ProjectResult.SourceFileResults.Count() == 0)
                {
                    _logger.LogError("Encountered errors during compilation in {0}.", project.ProjectName);
                    throw new PortingAssistantClientException($"Errors during compilation in {project.ProjectName}.", null);
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
                    return agg.Concat(cur.Value.Select(i => i.Namespace)).ToHashSet();
                });

                var nugetPackages = analyzer.ProjectResult.ExternalReferences.NugetReferences
                    .Select(InvocationExpressionModelToInvocations.ReferenceToPackageVersionPair)
                    .ToList();

                var subDependencies = analyzer.ProjectResult.ExternalReferences.NugetDependencies
                    .Select(InvocationExpressionModelToInvocations.ReferenceToPackageVersionPair)
                    .ToList();

                var sdkPackages = analyzer.ProjectResult.ExternalReferences.SdkReferences
                    .Select(InvocationExpressionModelToInvocations.ReferenceToPackageVersionPair)
                    .ToList();

                var allPackages = nugetPackages.Concat(subDependencies).Concat(sdkPackages).ToHashSet().ToList();

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
                    PackageAnalysisResults = packageAnalysisResults,
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
