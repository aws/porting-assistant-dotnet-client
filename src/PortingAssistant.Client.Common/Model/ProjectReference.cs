using System.IO;
using System.Xml.Linq;
using PortingAssistant.Client.Utils;

namespace PortingAssistant.Client.Model
{
    public class ProjectReference
    {
        public string ReferencePath { get; set; }

        public static ProjectReference Get(XElement packageReferenceElement, string projectAbsolutePath)
        {
            return new ProjectReference
            {
                ReferencePath = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(projectAbsolutePath),
                    packageReferenceElement.GetAttributeValue("Include")
                        .Replace("\\", Path.DirectorySeparatorChar.ToString())))
            };
        }
    }
}
