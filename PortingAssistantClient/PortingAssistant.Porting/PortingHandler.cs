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

        public PortingHandler(ILogger<PortingHandler> logger, IPortingProjectFileHandler portingProjectFileHandler)
        {
            _logger = logger;
            _portingProjectFileHandler = portingProjectFileHandler;
        }
        
        public List<PortingResult> ApplyPortProjectFileChanges(
            List<string> projectPaths, string solutionPath, string targetFramework,
            Dictionary<string, string> upgradeVersions)
        {
            return _portingProjectFileHandler.ApplyProjectChanges(projectPaths, solutionPath, targetFramework, upgradeVersions);
        }
    }
}
