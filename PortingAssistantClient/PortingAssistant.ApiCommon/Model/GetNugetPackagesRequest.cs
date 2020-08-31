using System.Collections.Generic;
using PortingAssistantCommon.Model;

namespace PortingAssistantApiCommon.Model
{
    public class GetNugetPackagesRequest
    {
        public List<PackageVersionPair> PackageVersions { get; set; }
        public string SolutionPath { get; set; }
    }
}
