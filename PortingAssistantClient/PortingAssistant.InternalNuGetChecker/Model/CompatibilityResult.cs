﻿using System.Collections.Generic;

namespace PortingAssistant.InternalNuGetChecker.Model
{
    public class CompatibilityResult
    {
        public bool IsCompatible { get; set; }
        public string source { get; set; }
        public List<string> IncompatibleDlls;
        public List<string> CompatibleDlls;
        public List<string> DepedencyPackages;
    }
}
