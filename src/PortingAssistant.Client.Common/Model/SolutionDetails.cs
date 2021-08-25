using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class SolutionDetails
    {
        public string SolutionName { get; set; }
        public string SolutionFilePath { get; set; }
        public string SolutionGuid { get; set; }
        /*
         * Used to uniquely identify the solution PA is analyzing.
         * Its value is the same as SolutionGuid if SolutionGuid presents.
         * Otherwise, we compute the hash based on all the ProjectGuids.
         */
        public string ApplicationGuid { get; set; }
        public string RepositoryUrl { get; set; }
        public List<string> FailedProjects { get; set; }
        public List<ProjectDetails> Projects { get; set; }
    }
}
