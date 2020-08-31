using PortingAssistantApiCommon.Model;
using PortingAssistantCommon.Model;

namespace PortingAssistantApiCommon.Listener
{
    public delegate void OnApiAnalysisUpdate(Response<ProjectAnalysisResult, SolutionProject> response);
}
