using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistantHandler.Model;
using PortingAssistant.Model;

namespace PortingAssistantHandler
{
    public interface IAssessmentHandler
    {
        List<Solution> GetSolutions(List<string> pathToSolutions);
        GetProjectResult GetProjects(string pathToSolution, bool projectsOnly);
        Dictionary<PackageVersionPair, Task<PackageVersionResult>> GetNugetPackages(List<PackageVersionPair> packageVersions, string solutionPath);
    }
}
