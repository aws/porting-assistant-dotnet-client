using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Model;

namespace PortingAssistant.NuGet
{
    public interface IPortingAssistantNamespaceHandler
    {
        public Dictionary<string, Task<NamespaceDetails>> GetNamespaceDetails(List<string> Namespace);
    }
}
