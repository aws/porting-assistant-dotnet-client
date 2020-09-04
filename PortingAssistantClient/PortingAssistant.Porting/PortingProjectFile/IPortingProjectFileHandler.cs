using System.Collections.Generic;
using PortingAssistant.Model;

namespace PortingAssistant.PortingProjectFile
{
    public interface IPortingProjectFileHandler
    {
        List<PortingProjectFileResult> ApplyProjectChanges(
            List<string> projectPaths, string solutionPath, string targetFramework,
            Dictionary<string, string> upgradeVersions);
    }
}
