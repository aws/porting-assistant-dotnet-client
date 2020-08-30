using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EncorePrivateCompatibilityCheck.Model;
using NuGet.Protocol.Core.Types;

namespace EncorePrivateCompatibilityCheck
{
    public interface IEncoreInternalCheckCompatibilityHandler
    {
        public Task<CompatibilityResult> CheckCompatibilityAsync(string packageName, string version, string targetFramework, IEnumerable<SourceRepository> internalRepositories);
    }
}