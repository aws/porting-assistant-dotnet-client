using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.NuGet
{
    public interface IPortingAssistantRecommendationHandler
    {
        public Dictionary<string, Task<RecommendationDetails>> GetApiRecommendation(IEnumerable<string> namespaces);
    }
}
