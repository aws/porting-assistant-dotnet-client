using System.Collections.Generic;
using EncoreApiCommon.Listener;
using EncoreApiCommon.Model;
using EncoreCommon.Model;

namespace EncoreApiCommon.Services
{
    public interface IAssessmentService
    {
        Response<Dictionary<string, Solution>, object> GetSolutions(GetSolutionsRequest request);
        Response<List<Project>, List<string>> GetProjects(GetProjectsRequest request);
        Response<string, string> GetNugetPackages(GetNugetPackagesRequest request);
        void AddApiAnalysisListener(OnApiAnalysisUpdate listener);
        void AddNugetPackageListener(OnNugetPackageUpdate listener);
    }
}
