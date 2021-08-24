using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class SolutionDetails
    {
        public string SolutionName { get; set; }
        public string SolutionFilePath { get; set; }
        public string SolutionGuid { get; set; }
        public string RepositoryUrl { get; set; }
        public List<string> FailedProjects { get; set; }
        public List<ProjectDetails> Projects { get; set; }
    }
}
