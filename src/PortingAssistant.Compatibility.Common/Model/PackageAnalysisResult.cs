using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Compatibility.Common.Model
{
    public class PackageAnalysisResult
    {
        public PackageVersionPair PackageVersionPair { get; set; }
        public Dictionary<string, CompatibilityResult> CompatibilityResults { get; set; } // Target Framework CompatibilityResults pair
        public Recommendations Recommendations { get; set; }
    }
}
