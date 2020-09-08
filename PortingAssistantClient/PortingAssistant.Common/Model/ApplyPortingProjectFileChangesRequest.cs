using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class ApplyPortingProjectFileChangesRequest
    {
        public List<string> ProjectPaths { get; set; }
        public string SolutionPath { get; set; }
        public string TargetFramework { get; set; }
        public Dictionary<string, string> UpgradeVersions { get; set; }
    }
}
