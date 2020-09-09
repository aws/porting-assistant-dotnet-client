using System;
using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class CompatibilityResult
    {
        public Compatibility Compatibility { get; set; }
        public List<string> CompatibleVersion { get; set; }
    }
}
