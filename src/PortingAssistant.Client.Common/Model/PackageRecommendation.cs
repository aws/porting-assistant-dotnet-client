using System;
using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class PackageRecommendation : RecommendedAction
    {
        public string PackageId { get; set; }
        public string Version { get; set; }
        public List<string> TargetVersions { get; set; }
    }
}
