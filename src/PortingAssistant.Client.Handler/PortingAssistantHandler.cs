﻿using System;
using System.Collections.Generic;
using System.Linq;
using PortingAssistant.Client.Analysis;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.Porting;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace PortingAssistant.Client.Handler
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

        public async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionFilePath, Settings settings)
        {
            try
            {
                var solution = SolutionFile.Parse(solutionFilePath);
                var failedProjects = new List<string>();

                var projects = solution.ProjectsInOrder.Where(p => 
                    (p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat || 
                    p.ProjectType == SolutionProjectType.WebProject) && 
                    (settings.IgnoreProjects == null ||
                    !settings.IgnoreProjects.Contains(p.AbsolutePath)))
                    .Select(p => p.AbsolutePath)
                    .ToList();

                var projectAnalysisResultTasks = _AnalysisHandler.AnalyzeSolution(solutionFilePath, projects);

                var projectAnalysisResults = await Task.WhenAll(projects.Select(async p =>
                {
                    try
                    {
                        var projectAnalysisResult = projectAnalysisResultTasks.GetValueOrDefault(p, null);
                        if (projectAnalysisResult != null)
                        {
                            await projectAnalysisResult;
                            if (projectAnalysisResult.IsCompletedSuccessfully)
                            {
                                return projectAnalysisResult.Result;
                            }
                            failedProjects.Add(p);
                        }
                        return null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to assess {0}, exception: {1}", p, ex);
                        failedProjects.Add(p);
                        return null;
                    }

                }).Where(p => p != null).ToList());

                var solutionDetails = new SolutionDetails
                {
                    SolutionName = Path.GetFileNameWithoutExtension(solutionFilePath),
                    SolutionFilePath = solutionFilePath,
                    Projects = projectAnalysisResults.Select(p => (ProjectDetails)p).ToList(),
                    FailedProjects = failedProjects
                };

                return new SolutionAnalysisResult
                {
                    FailedProjects = failedProjects,
                    SolutionDetails = solutionDetails,
                    ProjectAnalysisResults = projectAnalysisResults.ToList()
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
