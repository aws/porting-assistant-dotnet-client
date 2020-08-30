using EncoreApiCommon.Model;
using EncoreCommon.Model;

namespace EncoreApiCommon.Listener
{
    public delegate void OnApiAnalysisUpdate(Response<ProjectAnalysisResult, SolutionProject> response);
}
