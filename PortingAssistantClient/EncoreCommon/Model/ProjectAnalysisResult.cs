using System.Collections.Generic;

namespace EncoreCommon.Model
{
    public class ProjectAnalysisResult
    {
        public long ElapseTime { get; set; }
        public string SolutionFile { get; set; }
        public string ProjectFile { get; set; }
        public List<string> Errors { get; set; }
        public Dictionary<string, List<InvocationWithCompatibility>> SourceFileToInvocations { get; set; }
    }
}
