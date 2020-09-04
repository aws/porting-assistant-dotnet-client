using System.Collections.Generic;
using PortingAssistant.Model;

namespace PortingAssistant.Model
{
    public class GetProjectResult
    {
        public List<Project> Projects;
        public SolutionApiAnalysisResult ApiInvocations;
        public List<string> FailedProjects;
    }
}
