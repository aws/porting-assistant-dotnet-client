﻿using Codelyzer.Analysis.Model;
using CTA.Rules.Models;
using PortingAssistant.Client.Common.Model;
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
        public ExternalReferences ExternalReferences { get; set; }
        public ProjectCompatibilityResult ProjectCompatibilityResult { get; set; }
    }
}
