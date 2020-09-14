using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Amazon.S3.Transfer;
using PortingAssistant.Model;

namespace PortingAssistant.NuGet
{
    public class ExternalPackagesCompatibilityChecker : ExternalCompatibilityChecker
    {
        public override PackageSourceType CompatibilityCheckerType => PackageSourceType.NUGET;

        public ExternalPackagesCompatibilityChecker(
            ITransferUtility transferUtility,
            ILogger<ExternalCompatibilityChecker> logger,
            IOptions<AnalyzerConfiguration> options) 
            : base(transferUtility, logger, options)
        {
        }
    }
}