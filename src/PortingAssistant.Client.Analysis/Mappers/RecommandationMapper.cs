using PortingAssistant.Client.Model;
using PortingAssistant.Compatibility.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Client.Analysis.Mappers
{
    public static class RecommandationMapper
    {
        public static List<RecommendedAction> Convert(List<Recommendation> r)
        {
            
            List<RecommendedAction> actions = new List<RecommendedAction>();
            if (r == null) {
                return actions;
            }
            foreach (var action in r)
            {
                Enum.TryParse(action.RecommendedActionType.ToString(), out PortingAssistant.Client.Model.RecommendedActionType actionType);
                var recommendation = new RecommendedAction
                {
                    Description = action.Description,
                    RecommendedActionType = actionType
                };
                actions.Add(recommendation);
            }
            return actions;
        }

        public static List<RecommendedAction> ConvertToPackageRecommendation(List<Recommendation> r)
        {

            List<RecommendedAction> actions = new List<RecommendedAction>();
            if (r == null)
            {
                return actions;
            }
            foreach (var action in r)
            {
                Enum.TryParse(action.RecommendedActionType.ToString(), out PortingAssistant.Client.Model.RecommendedActionType actionType);
                var recommendation = new PackageRecommendation
                {
                    PackageId = action.PackageId,
                    //Version = action.Version,
                    TargetVersions = GetCompatibleVersionsWithoutPreReleases(action.TargetVersions),
                    Description = action.Description,
                    RecommendedActionType = actionType
                };
                actions.Add(recommendation);
            }
            return actions;
        }

        public static List<string> GetCompatibleVersionsWithoutPreReleases(List<string> compatibleVersions)
        {
            if (compatibleVersions == null) return compatibleVersions;
            return compatibleVersions.Where(v => !v.Contains("-")).ToList();
        }
    }
}
