using System.Collections.Generic;
using EncoreCommon.Model;

namespace EncoreAssessment.Model
{
    public class GetProjectResult
    {
        public List<Project> Projects;
        public SolutionAnalysisResult ApiInvocations;
        public List<string> FailedProjects;
    }
}
