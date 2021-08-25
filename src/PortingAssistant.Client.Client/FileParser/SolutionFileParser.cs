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
                    /*
                     * A regular expression pattern can include subexpressions, which are defined by enclosing a portion of the regular expression pattern in parentheses. Every such subexpression forms a group. The Groups property provides access to information about those subexpression matches where:
                     * 
                     * At index 0: the full match.
                     * At index 1: the contents of the first parentheses.
                     * At index 2: the contents of the second parentheses.
                     * …and so on…
                     * 
                     * We need the cotent matched by the first parentheses, hence access the value through Groups[1].
                     */
                    return m.Groups[1].Value.ToLower();
                }
            }
            return null;
        }
    }
}
