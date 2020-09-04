using System;
using System.Collections.Generic;
using PortingAssistant.Model;

namespace PortingAssistant.Porting
{
    public interface IPortingHandler
    {
        List<PortingProjectFileResult> ApplyPortProjectFileChanges(
            List<string> projectPaths,
            string solutionPath,
            string targetFramework,
            Dictionary<string, string> upgradeVersions);
    }
}
