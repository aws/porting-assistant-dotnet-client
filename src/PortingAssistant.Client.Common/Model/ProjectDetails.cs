using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class ProjectDetails
    {
        public string ProjectName { get; set; }
        public string ProjectFilePath { get; set; }
        public string ProjectGuid { get; set; }
        public string ProjectType { get; set; }
        public string FeatureType { get; set; }
        public List<string> TargetFrameworks { get; set; }
        public List<PackageVersionPair> PackageReferences { get; set; }
        public List<ProjectReference> ProjectReferences { get; set; }
        public bool IsBuildFailed { get; set; }
        public int LinesOfCode { get; set; }
    }
}
