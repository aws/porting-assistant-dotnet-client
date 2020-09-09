using System;
using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class PackageRecommendation : RecommendationAction
    {
        public List<string> TargetVersions { get; set; }
    }
}
