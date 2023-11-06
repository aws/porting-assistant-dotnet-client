using PortingAssistant.Compatibility.Common.Model;

namespace PortingAssistant.Compatibility.Common.Interface
{
    /// <summary>
    /// Represents a compatibility checker for packages
    /// </summary>
    public interface ICompatibilityCheckerHandler
    {
        /// <summary>
        /// Runs the compatibility check
        /// </summary>
        /// <param name="packageVersions">A collection of packages and their versions</param>
        /// <returns></returns>
        public CompatibilityCheckerResponse Check(CompatibilityCheckerRequest request, HashSet<string> fullSdks,
            string awsS3Bucket = null, string profile = null, string awsRegion = null, string awsKmsKey = null);

        public CompatibilityCheckerResponse Check(CompatibilityCheckerRequest request);
    }
}
