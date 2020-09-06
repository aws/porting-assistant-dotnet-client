using System;
namespace PortingAssistant.Model
{
    public class ApiRecommendation : RecommendationAction
    {
        public Invocation Invocation { get; set; }
        public (RecommendedActionType,string) UpgradeVersion { get; set; }
    }
}
