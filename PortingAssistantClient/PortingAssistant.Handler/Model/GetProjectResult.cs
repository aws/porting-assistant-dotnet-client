using System.Collections.Generic;
using PortingAssistant.Model;

namespace PortingAssistantHandler.Model
{
    public class GetProjectResult
    {
        public List<Project> Projects;
        public SolutionAnalysisResult ApiInvocations;
        public List<string> FailedProjects;
    }
}
