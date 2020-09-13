using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using PortingAssistant.Model;
using PortingAssistant.PortingProjectFile;

namespace PortingAssistant.Porting
{
    public class PortingHandler : IPortingHandler
    {
        private readonly ILogger _logger;
        private readonly IPortingProjectFileHandler _portingProjectFileHandler;
           
        /// <summary>
        /// Create an Instance of a PortingHandler
        /// </summary>
        /// <param name="logger">An ILogger object</param>
        /// <param name="portingProjectFileHandler">A instance of a handler object to run the porting</param>
        public PortingHandler(ILogger<PortingHandler> logger, IPortingProjectFileHandler portingProjectFileHandler)
        {
            _logger = logger;
            _portingProjectFileHandler = portingProjectFileHandler;
        }

        /// <summary>
        /// Ports a list of projects
        /// </summary>
        /// <param name="projectPaths">List of projects paths</param>
        /// <param name="solutionPath">Path to solution file</param>
        /// <param name="targetFramework">Target framework to be used when porting</param>
        /// <param name="upgradeVersions">List of key/value pairs where key is package and value is version number</param>
        /// <returns>A PortingProjectFileResult object, representing the result of the porting operation</returns>
        public List<PortingResult> ApplyPortProjectFileChanges(
            List<string> projectPaths, string solutionPath, string targetFramework,
            Dictionary<string, string> upgradeVersions)
        {
            return _portingProjectFileHandler.ApplyProjectChanges(projectPaths, solutionPath, targetFramework, upgradeVersions);
        }
    }
}
