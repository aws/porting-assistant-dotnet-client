using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Amazon.S3.Transfer;
using PortingAssistant.Model;

namespace PortingAssistant.NuGet
{
    public class ExternalPackagesCompatibilityChecker : ExternalCompatibilityChecker
    {
        public ExternalPackagesCompatibilityChecker(
            ITransferUtility transferUtility,
            ILogger<ExternalCompatibilityChecker> logger,
            IOptions<AnalyzerConfiguration> options
            ) : base(transferUtility, logger, options)
        {
        }

        public override PackageSourceType GetCompatibilityCheckerType()
        {
            return PackageSourceType.NUGET;
        }
    }

}