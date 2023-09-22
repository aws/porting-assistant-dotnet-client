using Microsoft.Extensions.Logging;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;

namespace PortingAssistant.Compatibility.Core.Checkers
{
    public class NugetCompatibilityChecker : ExternalCompatibilityChecker
    {
        public override PackageSourceType CompatibilityCheckerType => PackageSourceType.NUGET;
        public ILogger _logger;
        public NugetCompatibilityChecker(
            IHttpService httpService,
            ILogger<NugetCompatibilityChecker> logger
        )
            : base(httpService, logger)
        {
        }
    }
}
