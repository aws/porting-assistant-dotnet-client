using System;
using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class PackageRecommendation : RecommendationAction
    {
        public Dictionary<string, PackageCompatibilityInfo> TargetFrameworkCompatibleVersionPair { get; set; }
    }
}
