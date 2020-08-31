using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class ProjectDetails
    {
        public string SolutionPath { get; set; }
        public string ProjectName { get; set; }
        public string ProjectFilePath { get; set; }
        public string ProjectGuid { get; set; }
        public string ProjectType { get; set; }
        public List<string> TargetFrameworks { get; set; }
        public List<PackageVersionPair> PackageReferences { get; set; }
        public List<ProjectReference> ProjectReferences { get; set; }
    }
}
