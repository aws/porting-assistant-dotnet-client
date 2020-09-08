using System;
namespace PortingAssistant.Model
{
    public class ApiRecommendation : RecommendationAction
    {
        public Invocation Invocation { get; set; }
        public string UpgradeVersion { get; set; }
        public RecommendationAction recommendationType { get; set; }
    }
}
