using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class SolutionDetails
    {
        public string SolutionName { get; set; }
        public string SolutionFilePath { get; set; }
        public List<string> FailedProjects { get; set; }
        public List<ProjectDetails> Projects { get; set; }
    }
}
