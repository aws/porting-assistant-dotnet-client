using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Compatibility.Common.Utils
{
    public static class NugetVersionExtensions
    {
        private static readonly HashSet<string> ZeroVersions = new HashSet<string>
        {
            "0.0.0",
            "0.0.0.0"
        };

        public static bool IsZeroVersion(this NuGetVersion thisVersion)
        {
            return ZeroVersions.Contains(thisVersion.ToString());
        }

        public static bool IsGreaterThanOrEqualTo(this NuGetVersion thisVersion, string otherVersion)
        {
            if (NuGetVersion.TryParse(otherVersion, out var validOtherVersion))
            {

                return validOtherVersion.IsZeroVersion()
                       || thisVersion.IsGreaterThanOrEqualTo(validOtherVersion);
            }

            return false;
        }

        public static bool IsGreaterThanOrEqualTo(this NuGetVersion thisVersion, NuGetVersion otherVersion)
        {
            return thisVersion.CompareTo(otherVersion) >= 0;
        }

        public static bool IsGreaterThan(this NuGetVersion thisVersion, string otherVersion)
        {
            if (otherVersion == "0.0.0" || otherVersion == "0.0.0.0")
            {
                return true;
            }

            if (NuGetVersion.TryParse(otherVersion, out var validOtherVersion))
            {
                return thisVersion.IsGreaterThan(validOtherVersion);
            }

            return false;
        }

        public static bool IsGreaterThan(this NuGetVersion thisVersion, NuGetVersion otherVersion)
        {
            return thisVersion.CompareTo(otherVersion) > 0;
        }

        public static bool IsLessThanOrEqualTo(this NuGetVersion thisVersion, string otherVersion)
        {
            if (thisVersion.ToString() == "0.0.0" || thisVersion.ToString() == "0.0.0.0")
            {
                return true;
            }

            if (NuGetVersion.TryParse(otherVersion, out var validOtherVersion))
            {
                return thisVersion.IsLessThanOrEqualTo(validOtherVersion);
            }

            return false;
        }

        public static bool IsLessThanOrEqualTo(this NuGetVersion thisVersion, NuGetVersion otherVersion)
        {
            return thisVersion.CompareTo(otherVersion) <= 0;
        }

        public static bool HasSameMajorAs(this NuGetVersion thisVersion, string otherVersion)
        {
            if (NuGetVersion.TryParse(otherVersion, out var validOtherVersion))
            {
                return thisVersion.HasSameMajorAs(validOtherVersion);
            }

            return false;
        }

        public static bool HasSameMajorAs(this NuGetVersion thisVersion, NuGetVersion otherVersion)
        {
            return thisVersion.Major.CompareTo(otherVersion.Major) == 0;
        }

        public static IEnumerable<string> FindLowerOrEqualCompatibleVersions(this NuGetVersion thisVersion, IEnumerable<string> compatibleNugetVersions)
        {
            return compatibleNugetVersions.Where(v =>
            {
                if (NuGetVersion.TryParse(v, out var validNugetVersion))
                {
                    return validNugetVersion.IsLessThanOrEqualTo(thisVersion);
                }

                return false;
            });
        }

        public static bool HasLowerOrEqualCompatibleVersion(this NuGetVersion thisVersion, IEnumerable<string> compatibleNugetVersions)
        {
            return thisVersion.FindLowerOrEqualCompatibleVersions(compatibleNugetVersions).Any();
        }

        public static IEnumerable<string> FindGreaterCompatibleVersions(this NuGetVersion thisVersion, IEnumerable<string> compatibleNugetVersions)
        {
            return compatibleNugetVersions.Where(v =>
            {
                if (NuGetVersion.TryParse(v, out var validNugetVersion))
                {
                    return validNugetVersion.IsGreaterThan(thisVersion);
                }

                return false;
            });
        }
    }
}
