using System.Collections.Generic;
using System.Linq;
using NuGet.Versioning;

namespace PortingAssistant.Client.Analysis.Utils
{
    public class NugetVersionHelper
    {
        public static NuGetVersion GetMaxVersion(IEnumerable<string> nugetVersions)
        {
            var parsedVersions = nugetVersions.Select(v =>
            {
                if (NuGetVersion.TryParse(v, out var validVersion))
                {
                    return validVersion;
                }

                return null;
            }).Where(v => v!= null);

            // Returns null if there are no valid nugetVersions
            return GetMaxVersion(parsedVersions);
        }

        public static NuGetVersion GetMaxVersion(IEnumerable<NuGetVersion> nugetVersions)
        {
            return nugetVersions.Max();
        }
        
        public static bool HasLowerCompatibleVersionWithSameMajor(NuGetVersion nugetVersion, IEnumerable<string> compatibleNugetVersions)
        {
            return compatibleNugetVersions.Any(v =>
                nugetVersion.IsGreaterThanOrEqualTo(v)
                && nugetVersion.HasSameMajorAs(v)
            );
        }
    }
}
