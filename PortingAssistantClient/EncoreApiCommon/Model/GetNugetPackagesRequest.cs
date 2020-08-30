using System.Collections.Generic;
using EncoreCommon.Model;

namespace EncoreApiCommon.Model
{
    public class GetNugetPackagesRequest
    {
        public List<PackageVersionPair> PackageVersions { get; set; }
        public string SolutionPath { get; set; }
    }
}
