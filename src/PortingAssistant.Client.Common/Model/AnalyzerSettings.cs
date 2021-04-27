using System;
using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class AnalyzerSettings
    {
        public List<String> IgnoreProjects { get; set; }

        public string TargetFramework { get; set; }

        public bool ContiniousEnabled { get; set; }

        public bool CompatibleOnly { get; set; }

        public bool ActionsOnly { get; set; }
    }
}
