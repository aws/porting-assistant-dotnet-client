using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;

namespace PortingAssistant.Compatibility.Core.Checkers
{
    public class SdkCompatibilityChecker : ExternalCompatibilityChecker
    {
        public override PackageSourceType CompatibilityCheckerType => PackageSourceType.SDK;
        private ILogger _logger;
        public SdkCompatibilityChecker(
            IRegionalDatastoreService regionalDatastoreService,
            ILogger<SdkCompatibilityChecker> logger)
            : base(regionalDatastoreService, logger)
        {
        }
    }
}
