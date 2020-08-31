using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;
using PortingAssistant.InternalNuGetChecker.Model;

namespace PortingAssistant.InternalNuGetChecker
{
    public interface IPortingAssistantInternalNuGetCompatibilityHandler
    {
        public Task<CompatibilityResult> CheckCompatibilityAsync(string packageName, string version, string targetFramework, IEnumerable<SourceRepository> internalRepositories);
    }
}