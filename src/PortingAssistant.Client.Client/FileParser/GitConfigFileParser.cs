using System.IO;
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
            if (!Directory.Exists(gitRepositoryRootPath))
            {
                return null;
            }

            var repo = new Repository(gitRepositoryRootPath);
            return repo.Config.Get<string>(new[] { "remote", "origin", "url" }).Value;
        }
    }
}
