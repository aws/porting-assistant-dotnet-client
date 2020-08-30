using System.Collections.Generic;
using EncoreCommon.Model;

namespace EncoreApiAnalysis.Model
{
    public class EncoreApiCompatibilityRequest
    {
        public List<PackageVersionPair> NugetPackages { get; set; }
        public List<EncoreMethodSignature> MethodSignatures { get; set; }

        public class EncoreMethodSignature
        {
            public string MethodSignature { get; set; }
        }
    }

    
}
