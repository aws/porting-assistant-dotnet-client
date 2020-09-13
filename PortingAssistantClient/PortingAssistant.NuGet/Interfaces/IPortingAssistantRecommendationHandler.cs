using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Model;

namespace PortingAssistant.NuGet
{
    public interface IPortingAssistantRecommendationHandler
    {
        public Dictionary<string, Task<RecommendationDetails>> GetApiRecommendation(List<string> Namespace);
    }
}
