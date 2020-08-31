using System;
using System.Collections.Generic;
using System.Linq;
using PortingAssistantApiCommon.Listener;
using PortingAssistantApiCommon.Model;
using PortingAssistantAssessment;
using Microsoft.Extensions.Logging;
using PortingAssistantCommon.Model;

namespace PortingAssistantApiCommon.Services
{
    public class AssessmentService : IAssessmentService
    {
        private readonly ILogger _logger;
        private readonly IAssessmentHandler _handler;
        private static readonly List<OnApiAnalysisUpdate> _apiAnalysisListeners = new List<OnApiAnalysisUpdate>();
        private static readonly List<OnNugetPackageUpdate> _nugetPackageListeners = new List<OnNugetPackageUpdate>();

        public AssessmentService(ILogger<AssessmentService> logger,
            IAssessmentHandler handler)
        {
            _logger = logger;
            _handler = handler;
        }

        public Response<Dictionary<string, Solution>, object> GetSolutions(GetSolutionsRequest request)
        {
            try
            {
                return new Response<Dictionary<string, Solution>, object>
                {
                    Value = _handler.GetSolutions(request.SolutionPaths).ToDictionary(s => s.SolutionPath),
                    Status = Response<Dictionary<string, Solution>, object>.Success()
                };
            }
            catch (Exception ex)
            {
                return new Response<Dictionary<string, Solution>, object>
                {
                    Status = Response<Dictionary<string, Solution>, object>.Failed(ex),
                };
            }

        }

        public Response<List<Project>, List<string>> GetProjects(GetProjectsRequest request)
        {
            try
            {
                var results = _handler.GetProjects(request.SolutionPath, request.ProjectsOnly);

                if (!request.ProjectsOnly)
                {
                    results.ApiInvocations.ProjectAnalysisResults.ToList().ForEach((result) =>
                        result.Value.ContinueWith(apiAnalysis =>
                        {
                            if (apiAnalysis.IsCompletedSuccessfully)
                            {
                                _apiAnalysisListeners.ForEach(listener =>
                                    listener.Invoke(new Response<ProjectAnalysisResult, SolutionProject>
                                    {
                                        Value = apiAnalysis.Result,
                                        Status = Response<ProjectAnalysisResult, SolutionProject>.Success()
                                    }));
                                return;
                            }
                            _apiAnalysisListeners.ForEach(listener =>
                                    listener.Invoke(new Response<ProjectAnalysisResult, SolutionProject>
                                    {
                                        ErrorValue = new SolutionProject
                                        {
                                            ProjectPath = result.Key,
                                            SolutionPath = request.SolutionPath
                                        },
                                        Status = Response<ProjectAnalysisResult, SolutionProject>.Failed(apiAnalysis.Exception)
                                    }));
                        }));
                }

                return new Response<List<Project>, List<string>>
                {
                    Value = results.Projects,
                    Status = Response<List<Project>, List<string>>.Success(),
                    ErrorValue = results.FailedProjects
                };
            }
            catch (Exception ex)
            {
                return new Response<List<Project>, List<string>>
                {
                    Status = Response<List<Project>, List<string>>.Failed(ex)
                };
            }
        }

        public Response<string, string> GetNugetPackages(GetNugetPackagesRequest request)
        {
            try
            {
                var results = _handler.GetNugetPackages(request.PackageVersions, request.SolutionPath);
                results.ToList().ForEach(r =>
                {
                    r.Value.ContinueWith(result =>
                    {
                        if (result.IsCompletedSuccessfully)
                        {
                            _nugetPackageListeners.ForEach(l => l.Invoke(new Response<PackageVersionResult, PackageVersionPair>
                            {
                                Value = result.Result,
                                Status = Response<PackageVersionResult, PackageVersionPair>.Success()
                            }));
                            return;
                        }

                        _nugetPackageListeners.ForEach(l => l.Invoke(new Response<PackageVersionResult, PackageVersionPair>
                        {
                            ErrorValue = r.Key,
                            Status = Response<PackageVersionResult, PackageVersionPair>.Failed(result.Exception)
                        }));
                    });
                });
                return new Response<string, string>
                {
                    Value = request.SolutionPath,
                    Status = Response<string, string>.Success()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to query for Nuget Packages with exception", ex);
                return new Response<string, string>
                {
                    ErrorValue = request.SolutionPath,
                    Status = Response<string, string>.Failed(ex)
                };
            }
        }

        public void AddApiAnalysisListener(OnApiAnalysisUpdate listener)
        {
            _apiAnalysisListeners.Add(listener);
        }

        public void AddNugetPackageListener(OnNugetPackageUpdate listener)
        {
            _nugetPackageListeners.Add(listener);
        }
    }
}
