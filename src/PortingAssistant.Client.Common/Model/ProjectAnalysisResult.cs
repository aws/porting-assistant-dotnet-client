﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortingAssistant.Client.Model
{
    public class ProjectAnalysisResult : ProjectDetails
    {
        public List<string> Errors { get; set; }
        public bool IsBuildFailed { get; set; }
        public List<SourceFileAnalysisResult> SourceFileAnalysisResults { get; set; }
        public Dictionary<PackageVersionPair, Task<PackageAnalysisResult>> PackageAnalysisResults { get; set; }
    }
}
