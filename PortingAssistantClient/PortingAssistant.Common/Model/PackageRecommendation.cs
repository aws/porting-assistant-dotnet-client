using System;
using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class PackageRecommendation : Recommendation
    {
        Dictionary<string, PackageCompatibilityInfo> TargetFrameworkCompatibleVersionPair { get; set; }
    }
}
