using Microsoft.Extensions.Logging;
using PortingAssistant.Model;
using PortingAssistant.NuGet.Interfaces;

namespace PortingAssistant.NuGet
{
    public class SdkCompatibilityChecker : ExternalCompatibilityChecker
    {
        public override PackageSourceType CompatibilityCheckerType => PackageSourceType.SDK;

        public SdkCompatibilityChecker(
            IHttpService httpService,
            ILogger<ExternalCompatibilityChecker> logger)
            : base(httpService, logger)
        {
        }
    }
}