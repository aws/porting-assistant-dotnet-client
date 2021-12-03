using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace PortingAssistant.Client.Client.FileParser
{
    public class GitConfigFileParser
    {
        public static string getGitRepositoryRootPath(string solutionPath)
        {
            string dirName = Path.GetDirectoryName(solutionPath);
            if (System.String.IsNullOrWhiteSpace(dirName))
            {
                return null;
            }

            var gitRepoRootPath = Repository.Discover(dirName);

            if (gitRepoRootPath != null)
            {
                return gitRepoRootPath;
            }

            // The solution is not versioned by git
            return null;
        }

        public static string getGitRepositoryUrl(string gitRepositoryRootPath)
        {
            if (gitRepositoryRootPath == null || !Directory.Exists(gitRepositoryRootPath))
            {
                return null;
            }

            var repo = new Repository(gitRepositoryRootPath);

            var configEntries = new List<ConfigurationEntry<string>>(repo.Config.Find("remote.origin.url"));
            return configEntries.FirstOrDefault()?.Value;
        }
    }
}
