using System;
using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class AnalyzerSettings
    {
        public List<String> IgnoreProjects { get; set; }

        public string TargetFramework { get; set; }
    }
}
