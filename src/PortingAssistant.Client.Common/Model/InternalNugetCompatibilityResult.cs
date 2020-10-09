﻿using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class InternalNuGetCompatibilityResult
    {
        public bool IsCompatible { get; set; }
        public string Source { get; set; }
        public List<string> IncompatibleDlls;
        public List<string> CompatibleDlls;
        public List<string> DependencyPackages;
    }
}
