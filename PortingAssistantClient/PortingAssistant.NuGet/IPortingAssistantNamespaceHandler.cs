using System.Collections.Generic;
using System.Threading.Tasks;
using PortingAssistant.Model;

namespace PortingAssistant.NuGet
{
    public interface IPortingAssistantNamespaceHandler
    {
        public Dictionary<string, Task<PackageDetails>> GetNamespaceDetails(List<string> Namespace);
    }
}
