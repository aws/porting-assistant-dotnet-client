using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.NuGet.Interfaces;

namespace PortingAssistant.Client.NuGet
{
    public class ExternalPackagesCompatibilityChecker : ExternalCompatibilityChecker
    {
        public override PackageSourceType CompatibilityCheckerType => PackageSourceType.NUGET;

        public ExternalPackagesCompatibilityChecker(
            IHttpService httpService,
            ILogger<ExternalCompatibilityChecker> logger,
            IFileSystem fileSystem = null
            )
            : base(httpService, logger, fileSystem)
        {
        }
    }
}