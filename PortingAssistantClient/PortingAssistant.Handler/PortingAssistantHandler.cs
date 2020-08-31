using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buildalyzer;
using PortingAssistant.ApiAnalysis;
using PortingAssistantHandler.FileParser;
using PortingAssistantHandler.Model;
using PortingAssistant.NuGet;
using PortingAssistant.Model;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace PortingAssistantHandler
{
    public class AssessmentHandler : IAssessmentHandler
    {
        private readonly ILogger _logger;
        private readonly IPortingAssistantNuGetHandler _cache;
        private readonly IPortingAssistantApiAnalysisHandler _apiAnalysis;

        public AssessmentHandler(ILogger<AssessmentHandler> logger,
            IPortingAssistantNuGetHandler cache,
            IPortingAssistantApiAnalysisHandler apiAnalysis)
        {
            _logger = logger;
            _cache = cache;
            _apiAnalysis = apiAnalysis;
        }

        public List<Solution> GetSolutions(List<string> pathToSolutions)
        {
            var solutions = new List<Solution>();
            var failedSolutions = new Dictionary<string, Exception>();
            foreach (var pathToSolution in pathToSolutions)
            {
                try
                {
                    var solution = SolutionFile.Parse(pathToSolution);
                    solutions.Add(new Solution
                    {
                        SolutionPath = pathToSolution,
                        NumProjects = solution.ProjectsInOrder
                            .Where(p => p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                            .Count()
                    });
                }
                catch (Exception ex)
                {
                    failedSolutions.Add(pathToSolution, ex);
                }
            }
            return solutions;
        }

        public GetProjectResult GetProjects(string pathToSolution, bool projectsOnly)
        {
            var manager = new AnalyzerManager(pathToSolution);
            var solution = SolutionFile.Parse(pathToSolution);
            var failedProjects = new List<string>();

            var projects = solution.ProjectsInOrder.Select(p =>
            {
                if (p.ProjectType != SolutionProjectType.KnownToBeMSBuildFormat && p.ProjectType != SolutionProjectType.WebProject)
                {
                    return null;
                }

                _logger.LogInformation("Analyzing: {0}", p.ProjectName);

                try
                {
                    var projectParser = new ProjectFileParser(p.AbsolutePath);

                    return new Project
                    {
                        ProjectName = p.ProjectName,
                        SolutionPath = pathToSolution,
                        ProjectPath = p.AbsolutePath,
                        ProjectGuid = p.ProjectGuid,
                        TargetFrameworks = projectParser.GetTargetFrameworks().Select(tfm =>
                        {
                            var framework = NuGetFramework.Parse(tfm);
                            return string.Format("{0} {1}", framework.Framework, NuGetVersion.Parse(framework.Version.ToString()).ToNormalizedString());
                        }).ToList(),
                        NugetDependencies = projectParser.GetPackageReferences(),
                        ProjectReferences = projectParser.GetProjectReferences()
                    };
                }
                catch (Exception ex)
                {
                    failedProjects.Add(p.AbsolutePath);
                    _logger.LogWarning("Failed to assess {0}, exception: {1}", p.ProjectName, ex);
                    return null;
                }
            }).Where(p => p != null).ToList();

            if (!projectsOnly)
            {

                var apiCompatibilityResults = _apiAnalysis.AnalyzeSolution(pathToSolution, projects);

                return new GetProjectResult
                {
                    Projects = projects,
                    ApiInvocations = apiCompatibilityResults,
                    FailedProjects = failedProjects
                };
            }
            else
            {
                return new GetProjectResult
                {
                    Projects = projects,
                    FailedProjects = failedProjects
                };
            }
        }

        public Dictionary<PackageVersionPair, Task<PackageVersionResult>> GetNugetPackages(List<PackageVersionPair> packageVersions, string pathToSolution)
        {
            return _cache.GetNugetPackages(packageVersions, pathToSolution);
        }
    }
}
