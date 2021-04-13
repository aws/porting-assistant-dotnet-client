using CTA.Rules.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortingAssistant.Client.Model
{
    public class ProjectAnalysisResult : ProjectDetails
    {
        public List<string> Errors { get; set; }
        public List<SourceFileAnalysisResult> SourceFileAnalysisResults { get; set; }
        public Dictionary<PackageVersionPair, Task<PackageAnalysisResult>> PackageAnalysisResults { get; set; }
        public List<string> PreportMetaReferences { get; set; }
        public List<string> MetaReferences { get; set; }
        public RootNodes ProjectRules { get; set; }
    }
}
