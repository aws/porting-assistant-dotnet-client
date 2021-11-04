using System;
using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class ProjectApiAnalysisResult : IDisposable
    {
        public string SolutionFile { get; set; }
        public string SolutionGuid { get; set; }
        public string ApplicationGuid { get; set; }
        public string RepositoryUrl { get; set; }
        public string ProjectFile { get; set; }
        public string ProjectGuid { get; set; }
        public List<string> Errors { get; set; }
        public string SchemaVersion { get; set; }
        public List<SourceFileAnalysisResult> SourceFileAnalysisResults { get; set; }

        public void Dispose()
        {
            SourceFileAnalysisResults = null;
            Errors = null;
        }
    }
}