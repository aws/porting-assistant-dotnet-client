using Microsoft.Extensions.Logging;
using PortingAssistant.Model;
using PortingAssistant.NuGet.Interfaces;

namespace PortingAssistant.NuGet
{
    public class ExternalPackagesCompatibilityChecker : ExternalCompatibilityChecker
    {
        public override PackageSourceType CompatibilityCheckerType => PackageSourceType.NUGET;

        public ExternalPackagesCompatibilityChecker(
            IHttpService httpService,
            ILogger<ExternalCompatibilityChecker> logger
            )
            : base(httpService, logger)
        {
        }
    }
}