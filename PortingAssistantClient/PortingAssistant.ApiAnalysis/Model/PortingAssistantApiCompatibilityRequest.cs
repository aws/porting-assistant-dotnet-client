using System.Collections.Generic;
using PortingAssistant.Model;

namespace PortingAssistant.ApiAnalysis.Model
{
    public class PortingAssistantApiCompatibilityRequest
    {
        public List<PackageVersionPair> NugetPackages { get; set; }
        public List<PortingAssistantMethodSignature> MethodSignatures { get; set; }

        public class PortingAssistantMethodSignature
        {
            public string MethodSignature { get; set; }
        }
    }


}
