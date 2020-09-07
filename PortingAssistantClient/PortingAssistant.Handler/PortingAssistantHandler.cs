using System;
using System.Collections.Generic;
using System.Linq;
using PortingAssistant.ApiAnalysis;
using PortingAssistant.FileParser;
using PortingAssistant.NuGet;
using PortingAssistant.Model;
using PortingAssistant.Utils;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Versioning;
using System.IO;

namespace PortingAssistant
{
    public class AssessmentHandler : IAssessmentHandler
    {
        private readonly ILogger _logger;
        private readonly IPortingAssistantNuGetHandler _handler;
        private readonly IPortingAssistantApiAnalysisHandler _apiAnalysis;

        public AssessmentHandler(ILogger<AssessmentHandler> logger,
            IPortingAssistantNuGetHandler handler,
            IPortingAssistantApiAnalysisHandler apiAnalysis)
        {
            _logger = logger;
            _handler = handler;
            _apiAnalysis = apiAnalysis;
        }

        public SolutionDetails GetSolutionDetails(string solutionFilePath)
        {
            var solution = SolutionFile.Parse(solutionFilePath);
            var failedProjects = new List<string>();

            var Projects = solution.ProjectsInOrder
                        .Where(p => p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat || p.ProjectType == SolutionProjectType.WebProject)
                        .Select(p =>
                        {
                            _logger.LogInformation("Analyzing: {0}", p.ProjectName);
                            try
                            {
                                var projectParser = new ProjectFileParser(p.AbsolutePath);

                                return new ProjectDetails
                                {
                                    ProjectName = p.ProjectName,
                                    ProjectFilePath = p.AbsolutePath,
                                    ProjectGuid = p.ProjectGuid,
                                    ProjectType = p.ProjectType.ToString(),
                                    TargetFrameworks = projectParser.GetTargetFrameworks().Select(tfm =>
                                    {
                                        var framework = NuGetFramework.Parse(tfm);
                                        return string.Format("{0} {1}", framework.Framework, NuGetVersion.Parse(framework.Version.ToString()).ToNormalizedString());
                                    }).ToList(),
                                    PackageReferences = projectParser.GetPackageReferences(),
                                    ProjectReferences = projectParser.GetProjectReferences(),

                                };
                            }
                            catch (Exception ex)
                            {
                                failedProjects.Add(p.AbsolutePath);
                                _logger.LogWarning("Failed to assess {0}, exception: {1}", p.ProjectName, ex);
                                return null;
                            }

                        }).Where(p => p != null).ToList();

            return new SolutionDetails
            {
                SolutionName = Path.GetFileNameWithoutExtension(solutionFilePath),
                SolutionFilePath = solutionFilePath,
                Projects = Projects,
                FailedProjects = failedProjects
            };
        }

        public SolutionAnalysisResult AnalyzeSolution(string solutionFilePath)
        {
            var solutionDetails = GetSolutionDetails(solutionFilePath);
            var solutionApiAnalysisResult = _apiAnalysis.AnalyzeSolution(solutionFilePath, solutionDetails.Projects);

            var solutionAnalysisResult = new SolutionAnalysisResult
            {
                FailedProjects = solutionDetails.FailedProjects,
                SolutionDetails = solutionDetails,
                ProjectAnalysisResult = solutionDetails.Projects.Select(p =>
                {
                    var projectApiAnalysisResult = solutionApiAnalysisResult.ProjectApiAnalysisResults.GetValueOrDefault(p.ProjectFilePath);
                    projectApiAnalysisResult.Wait();
                    var packageAnalysisResults = _handler.GetNugetPackages(p.PackageReferences, solutionFilePath)
                                                    .Select(package =>
                                                    {
                                                        var result = PackageCompatibility.isCompatibleAsync(package.Value, package.Key, _logger);
                                                        return PackageCompatibility.GetPackageAnalysisResult(result, package.Key);
                                                    }).Where(p => p != null).ToList();
                    return new ProjectAnalysisResult
                    {
                        ProjectFile = p.ProjectFilePath,
                        ProjectName = p.ProjectName,
                        ProjectApiAnalysisResult = projectApiAnalysisResult,
                        PackageAnalysisResults = packageAnalysisResults
                    };
                }).ToList()

            };

            return solutionAnalysisResult;
        }
    }
}
