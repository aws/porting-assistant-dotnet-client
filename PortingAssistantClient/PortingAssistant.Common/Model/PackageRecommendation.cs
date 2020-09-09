using System;
using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class PackageRecommendation : RecommendedAction
    {
        public string PackageId { get; set; }
        public List<string> TargetVersions { get; set; }
    }
}
