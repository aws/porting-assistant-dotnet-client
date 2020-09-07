using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwsCodeAnalyzer;
using AwsCodeAnalyzer.Model;
using PortingAssistantApiAnalysis.Utils;
using PortingAssistant.Model;
using Microsoft.Extensions.Logging;
using Serilog;
using PortingAssistant.NuGet;
using PortingAssistant.ApiAnalysis.Utils;
using AnalyzerConfiguration = AwsCodeAnalyzer.AnalyzerConfiguration;

namespace PortingAssistant.ApiAnalysis
{
    public class PortingAssistantApiAnalysisHandler : IPortingAssistantApiAnalysisHandler
    {
        private readonly ILogger<PortingAssistantApiAnalysisHandler> _logger;
        private readonly IPortingAssistantNuGetHandler _handler;

        private static readonly int _maxBuildConcurrency = 1;
        private static readonly SemaphoreSlim _buildConcurrency = new SemaphoreSlim(_maxBuildConcurrency);

        public PortingAssistantApiAnalysisHandler(ILogger<PortingAssistantApiAnalysisHandler> logger,
            IPortingAssistantNuGetHandler handler)
        {
            _logger = logger;
            _handler = handler;
        }

        public SolutionApiAnalysisResult AnalyzeSolution(
            string solutionFilename, List<ProjectDetails> projects)
        {
            var options = new AnalyzerConfiguration(LanguageOptions.CSharp)
            {
                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true
                }
            };
            var analyzer = CodeAnalyzerFactory.GetAnalyzer(options, Log.Logger);
            var analyzersTask = analyzer.AnalyzeSolution(solutionFilename);

            return new SolutionApiAnalysisResult
            {
                ProjectApiAnalysisResults = projects
                    .Select((project) => AnalyzeProject(solutionFilename, project, analyzersTask))
                    .Where(p => p.Value != null)
                    .ToDictionary(p => p.Key, p => p.Value)
            };
        }

        private KeyValuePair<string, Task<ProjectApiAnalysisResult>> AnalyzeProject(
            string solutionFilename, ProjectDetails project, Task<List<AnalyzerResult>> analyzersTask)
        {
            var task = AnalyzeProjectAsync(solutionFilename, project, analyzersTask);
            return KeyValuePair.Create(project.ProjectFilePath, task);
        }

        private async Task<ProjectApiAnalysisResult> AnalyzeProjectAsync(
            string solutionFilename, ProjectDetails project, Task<List<AnalyzerResult>> analyzersTask)
        {
            try
            {
                var startTime = DateTime.Now.Ticks;
                var analyzers = await analyzersTask;
                var invocationsMethodSignatures = new HashSet<string>();
                var SemanticNamespaces = new HashSet<string>();

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
                    var invocations = FilterInternalInvocations.Filter(invocationsInSourceFile, project);
                    invocationsMethodSignatures.UnionWith(invocations.Select(invocation => invocation.SemanticOriginalDefinition));
                    SemanticNamespaces.UnionWith(invocations.Select(invocation => invocation.SemanticNamespace));
                    return KeyValuePair.Create(sourceFile.FileFullPath, invocations);
                }).ToDictionary(p => p.Key, p => p.Value);

                _logger.LogInformation("API: Project {0} has {1} invocations", project.ProjectName, invocationsMethodSignatures.Count());

                var fakePackages = SemanticNamespaces.Select(Namespace => {
                    return new PackageVersionPair
                    {
                        PackageId = Namespace
                    };
                }).ToList();
                var nameSpaceresults = _handler.GetNugetPackages(fakePackages, null);
                var SourceFileAnalysisResults = InvocationExpressionModelToInvocations.Convert(
                    sourceFileToInvocations, project, _handler, nameSpaceresults);

                return new ProjectApiAnalysisResult
                {
                    Errors = analyzer.ProjectResult.BuildErrors,
                    SourceFileAnalysisResults = SourceFileAnalysisResults,
                };
            }
            catch (PortingAssistantClientException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while analyzing {0}, {1}", project.ProjectName, ex);
                throw new PortingAssistantException($"Error while analyzing {project.ProjectName}", ex);
            }
        }
    }
}
