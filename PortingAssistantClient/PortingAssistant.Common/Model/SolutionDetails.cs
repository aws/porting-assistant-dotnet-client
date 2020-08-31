using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class SolutionDetails
    {
        public string SolutionName { get; set; }
        public string SolutionFilePath { get; set; }
        public string SolutionGuid { get; set; }
        public List<ProjectDetails> Projects { get; set; }
    }
}
