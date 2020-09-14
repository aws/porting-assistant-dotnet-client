using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Amazon.S3.Transfer;
using PortingAssistant.Model;

namespace PortingAssistant.NuGet
{
    public class NamespacesCompatibilityChecker : ExternalCompatibilityChecker
    {
        public override PackageSourceType CompatibilityCheckerType => PackageSourceType.SDK;

        public NamespacesCompatibilityChecker(
            ITransferUtility transferUtility,
            ILogger<ExternalCompatibilityChecker> logger,
            IOptions<AnalyzerConfiguration> options) 
            : base(transferUtility, logger, options)
        {
        }
    }
}