using System;
using System.Collections.Generic;
using System.Linq;
using PortingAssistant.Analysis;
using PortingAssistant.Handler.FileParser;
using PortingAssistant.NuGet;
using PortingAssistant.Model;
using PortingAssistant.Utils;
using PortingAssistant.Porting;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Versioning;
using System.IO;
using System.Threading.Tasks;

namespace PortingAssistant.Handler
{
    public class PortingAssistantHandler : IPortingAssistantHandler
    {
        private readonly ILogger _logger;
        private readonly IPortingAssistantAnalysisHandler _AnalysisHandler;
        private readonly IPortingHandler _portingHandler;

        public PortingAssistantHandler(ILogger<PortingAssistantHandler> logger,
            IPortingAssistantAnalysisHandler AnalysisHandler,
            IPortingHandler portingHandler
            )
        {
            _logger = logger;
            _AnalysisHandler = AnalysisHandler;
            _portingHandler = portingHandler;
        }

        public SolutionDetails GetSolutionDetails(string solutionFilePath)
        {
            try
            {
                var solution = SolutionFile.Parse(solutionFilePath);
                var failedProjects = new List<string>();

                var projects = solution.ProjectsInOrder
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
                    Projects = projects,
                    FailedProjects = failedProjects
                };
            }
            catch (Exception ex)
            {
                throw new PortingAssistantException($"Cannot Analyze solution {solutionFilePath}", ex);
            }
            
        }

        public SolutionAnalysisResult AnalyzeSolution(string solutionFilePath, Settings settings)
        {
            try
            {
                var solutionDetails = GetSolutionDetails(solutionFilePath);
                var projects = solutionDetails.Projects
                    .Where(p => settings.IgnoreProjects == null || !settings.IgnoreProjects.Contains(p.ProjectFilePath))
                    .ToList();
                var projectAnalysisResults = _AnalysisHandler.AnalyzeSolution(solutionFilePath, projects);

                return new SolutionAnalysisResult
                {
                    FailedProjects = solutionDetails.FailedProjects,
                    SolutionDetails = solutionDetails,
                    ProjectAnalysisResult = projectAnalysisResults.Select(dic => dic.Value).ToList()
                };

            }
            catch (Exception ex)
            {
                throw new PortingAssistantException($"Cannot Analyze solution {solutionFilePath}", ex);
            } 

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
                        return new Tuple<string, string>(packageRecommendation.PackageId, packageRecommendation.TargetVersions.First());
                    })
                    .ToDictionary(t => t.Item1, t => t.Item2);

                return _portingHandler.ApplyPortProjectFileChanges(
                    request.ProjectPaths, 
                    request.SolutionPath,
                    request.TargetFramework, 
                    upgradeVersions);
            }
            catch (Exception ex)
            {
                throw new PortingAssistantException("Could not apply porting changes", ex);
            }

        }
    }
}
