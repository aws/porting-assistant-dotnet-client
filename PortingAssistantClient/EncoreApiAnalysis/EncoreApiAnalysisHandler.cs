using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwsCodeAnalyzer;
using AwsCodeAnalyzer.Common;
using AwsCodeAnalyzer.Model;
using EncoreApiAnalysis.Utils;
using EncoreCommon.Model;
using Microsoft.Extensions.Logging;
using Serilog;
using EncoreCache;

namespace EncoreApiAnalysis
{
    public class EncoreApiAnalysisHandler : IEncoreApiAnalysisHandler
    {
        private readonly ILogger<EncoreApiAnalysisHandler> _logger;
        private readonly IEncoreCacheHandler _hanler;
        private static readonly int _maxBuildConcurrency = 1;
        private static readonly SemaphoreSlim _buildConcurrency = new SemaphoreSlim(_maxBuildConcurrency);

        public EncoreApiAnalysisHandler(ILogger<EncoreApiAnalysisHandler> logger, IEncoreCacheHandler handler)
        {
            _logger = logger;
            _hanler = handler;
        }

        public SolutionAnalysisResult AnalyzeSolution(
            string solutionFilename, List<Project> projects)
        {
            var options = new AnalyzerConfiguration(LanguageOptions.CSharp) {
                MetaDataSettings =
                {
                    LiteralExpressions = true,
                    MethodInvocations = true
                }
            };
            var analyzer = CodeAnalyzerFactory.GetAnalyzer(options, Log.Logger);
            var analyzersTask = analyzer.AnalyzeSolution(solutionFilename);

            return new SolutionAnalysisResult
            {
                ProjectAnalysisResults = projects
                    .Select((project) => AnalyzeProject(solutionFilename, project, analyzersTask))
                    .ToDictionary(p => p.Key, p => p.Value)
            };
        }

        private KeyValuePair<string, Task<ProjectAnalysisResult>> AnalyzeProject(
            string solutionFilename, Project project, Task<List<AnalyzerResult>> analyzersTask)
        {
            var task = AnalyzeProjectAsync(solutionFilename, project, analyzersTask);
            return KeyValuePair.Create(project.ProjectPath, task);
        }

        private async Task<ProjectAnalysisResult> AnalyzeProjectAsync(
            string solutionFilename, Project project, Task<List<AnalyzerResult>> analyzersTask)
        {
            try
            {
                var startTime = DateTime.Now.Ticks;
                var analyzers = await analyzersTask;
                var invocationsMethodSignatures = new HashSet<string>();

                var analyzer = analyzers.Find((a) => a.ProjectResult.ProjectFilePath.Equals(project.ProjectPath));

                if (analyzer == null || analyzer.ProjectResult == null)
                {
                    _logger.LogError("Unable to build {0}.", project.ProjectName);
                    throw new ApiAnalysisException(solutionFilename, project.ProjectPath, null, null);
                }

                if (analyzer.ProjectResult.BuildErrorsCount > 0 && analyzer.ProjectResult.SourceFileResults.Count() == 0)
                {
                    _logger.LogError("Encountered errors during compilation in {0}.", project.ProjectName);
                    throw new ApiAnalysisException(solutionFilename, project.ProjectPath, analyzer.ProjectResult.BuildErrors, null);
                }

                var sourceFileToInvocations = analyzer.ProjectResult.SourceFileResults.Select((sourceFile) =>
                {
                    var invocationsInSourceFile = sourceFile.AllInvocationExpressions();
                    _logger.LogInformation("API: SourceFile {0} has {1} invocations pre-filter", sourceFile.FileFullPath, invocationsInSourceFile.Count());
                    var invocations = FilterInternalInvocations.Filter(invocationsInSourceFile, project);
                    invocationsMethodSignatures.UnionWith(invocations.Select(invocation => invocation.SemanticOriginalDefinition));
                    return KeyValuePair.Create(sourceFile.FileFullPath, invocations);
                }).ToDictionary(p => p.Key, p => p.Value);

                _logger.LogInformation("API: Project {0} has {1} invocations", project.ProjectName, invocationsMethodSignatures.Count());

                var invocationsWithCompatibility = InvocationExpressionModelToInvocations.Convert(
                    sourceFileToInvocations, project, _hanler);

                return new ProjectAnalysisResult
                {
                    ElapseTime = DateTime.Now.Ticks - startTime,
                    SolutionFile = solutionFilename,
                    ProjectFile = project.ProjectPath,
                    Errors = analyzer.ProjectResult.BuildErrors,
                    SourceFileToInvocations = invocationsWithCompatibility,
                };
            }
            catch (ApiAnalysisException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while analyzing {0}, {1}", project.ProjectName, ex);
                throw new ApiAnalysisException(solutionFilename, project.ProjectPath, null, ex); ;
            }
        }
    }
}
