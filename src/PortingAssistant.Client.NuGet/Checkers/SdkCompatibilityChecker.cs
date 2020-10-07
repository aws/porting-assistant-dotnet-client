using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.NuGet.Interfaces;

namespace PortingAssistant.Client.NuGet
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