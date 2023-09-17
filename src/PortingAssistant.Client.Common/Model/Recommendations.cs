using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class Recommendations
    {
        public List<RecommendedAction> RecommendedActions { get; set; }
        public List<string> RecommendedPackageVersions { get; set; }
    }
}
