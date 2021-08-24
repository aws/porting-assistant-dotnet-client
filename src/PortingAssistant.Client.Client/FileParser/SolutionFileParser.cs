using System.IO;
using System.Text.RegularExpressions;

namespace PortingAssistant.Client.Client.FileParser
{
    public class SolutionFileParser
    {
        public static string getSolutionGuid(string solutionPath)
        {
            if (!File.Exists(solutionPath))
            {
                return null;
            }
            if (solutionPath.EndsWith(".sln"))
            {
                Regex rx = new Regex(".*SolutionGuid = {(.*)}");
                string solutionFile = File.ReadAllText(solutionPath);
                Match m = rx.Match(solutionFile);
                if (m.Success)
                {
                    return m.Groups[1].Value.ToLower();
                }
            }
            return null;
        }
    }
}
