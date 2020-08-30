using System.Collections.Generic;

namespace EncoreCommon.Model
{
    public class Project
    {
        public string ProjectName { get; set; }
        public string SolutionPath { get; set; }
        public string ProjectPath { get; set; }
        public string ProjectGuid { get; set; }
        public string ProjectType { get; set; }
        public List<string> TargetFrameworks { get; set; }
        public List<PackageVersionPair> NugetDependencies { get; set; }
        public List<ProjectReference> ProjectReferences { get; set; }
    }
}
