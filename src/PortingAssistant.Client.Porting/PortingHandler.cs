using System;
using System.Collections.Generic;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.PortingProjectFile;

namespace PortingAssistant.Client.Porting
{
    public class PortingHandler : IPortingHandler
    {
        private readonly IPortingProjectFileHandler _portingProjectFileHandler;

        /// <summary>
        /// Create an Instance of a PortingHandler
        /// </summary>
        /// <param name="logger">An ILogger object</param>
        /// <param name="portingProjectFileHandler">A instance of a handler object to run the porting</param>
        public PortingHandler(IPortingProjectFileHandler portingProjectFileHandler)
        {
            _portingProjectFileHandler = portingProjectFileHandler;
        }

        /// <summary>
        /// Ports a list of projects
        /// </summary>
        /// <param name="projectPaths">List of projects paths</param>
        /// <param name="solutionPath">Path to solution file</param>
        /// <param name="targetFramework">Target framework to be used when porting</param>
        /// <param name="upgradeVersions">List of key/value pairs where key is package and value is version number tuple<old, new></param>
        /// <returns>A PortingProjectFileResult object, representing the result of the porting operation</returns>
        public List<PortingResult> ApplyPortProjectFileChanges(
            List<ProjectDetails> projects, string solutionPath, string targetFramework,
            Dictionary<string, Tuple<string, string>> upgradeVersions)
        {
            return ApplyPortProjectFileChanges(projects, solutionPath, targetFramework, true, upgradeVersions);
        }

        /// <summary>
        /// Ports a list of projects
        /// </summary>
        /// <param name="projectPaths">List of projects paths</param>
        /// <param name="solutionPath">Path to solution file</param>
        /// <param name="targetFramework">Target framework to be used when porting</param>
        /// <param name="upgradeVersions">List of key/value pairs where key is package and value is version number tuple<old, new></param>
        /// <returns>A PortingProjectFileResult object, representing the result of the porting operation</returns>
        public List<PortingResult> ApplyPortProjectFileChanges(
            List<ProjectDetails> projects, string solutionPath, string targetFramework,
            bool includeCodeFix,
            Dictionary<string, Tuple<string, string>> upgradeVersions)
        {
            return _portingProjectFileHandler.ApplyProjectChanges(projects, solutionPath, targetFramework, includeCodeFix, upgradeVersions);
        }

    }
}
