using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.NuGet.InternalNuGet
{
    public interface IPortingAssistantInternalNuGetCompatibilityHandler
    {
        public Task<InternalNuGetCompatibilityResult> CheckCompatibilityAsync(
            string packageName, string version, string targetFramework, IEnumerable<SourceRepository> internalRepositories);
    }
}