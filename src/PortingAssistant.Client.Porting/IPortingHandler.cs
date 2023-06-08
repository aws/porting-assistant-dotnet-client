using System;
using System.Collections.Generic;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Porting
{
    /// <summary>     
    /// Represents a handler to port projects     
    /// </summary>
    public interface IPortingHandler
    {
        /// <summary>
        /// Ports a list of projects
        /// </summary>
        /// <param name="projects">List of projects</param>
        /// <param name="solutionPath">Path to solution file</param>
        /// <param name="targetFramework">Target framework to be used when porting</param>
        /// <param name="upgradeVersions">List of key/value pairs where key is package and value is version number tuple <old, new></param>
        /// <returns>A PortingResult object, representing the result of the porting operation</returns>
        List<PortingResult> ApplyPortProjectFileChanges(
            List<ProjectDetails> projects,
            string solutionPath,
            string targetFramework,
            Dictionary<string, Tuple<string, string>> upgradeVersions, VisualStudioVersion? visualStudioVersion = null);

        /// <summary>
        /// Ports a list of projects
        /// </summary>
        /// <param name="projects">List of projects</param>
        /// <param name="solutionPath">Path to solution file</param>
        /// <param name="targetFramework">Target framework to be used when porting</param>
        /// <param name="upgradeVersions">List of key/value pairs where key is package and value is version number tuple <old, new></param>
        /// <returns>A PortingResult object, representing the result of the porting operation</returns>
        List<PortingResult> ApplyPortProjectFileChanges(
            List<ProjectDetails> projects,
            string solutionPath,
            string targetFramework,
            bool includeCodeFix,
            Dictionary<string, Tuple<string, string>> upgradeVersions, VisualStudioVersion? visualStudioVersion = null);
    }
}
