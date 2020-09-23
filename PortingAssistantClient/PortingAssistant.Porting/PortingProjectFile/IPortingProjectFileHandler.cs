using System.Collections.Generic;
using PortingAssistant.Model;

namespace PortingAssistant.PortingProjectFile
{
    /// <summary>
    /// Represents a handler to port projects
    /// </summary>
    public interface IPortingProjectFileHandler
    {
        /// <summary>
        /// Ports a list of projects
        /// </summary>
        /// <param name="projectPaths">List of projects paths</param>
        /// <param name="solutionPath">Path to solution file</param>
        /// <param name="targetFramework">Target framework to be used when porting</param>
        /// <param name="upgradeVersions">List of key/value pairs where key is package and value is version number</param>
        /// <returns>A PortingProjectFileResult object, representing the result of the porting operation</returns>
        List<PortingResult> ApplyProjectChanges(
            List<string> projectPaths, string solutionPath, string targetFramework,
            Dictionary<string, string> upgradeVersions);
    }
}
