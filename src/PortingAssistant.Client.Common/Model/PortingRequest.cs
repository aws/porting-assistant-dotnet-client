using System;
using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class PortingRequest
    {
        public List<ProjectDetails> Projects { get; set; }
        public string SolutionPath { get; set; }
        public string TargetFramework { get; set; }
        public List<RecommendedAction> RecommendedActions { get; set; }
        public bool IncludeCodeFix { get; set; }
    }
}
