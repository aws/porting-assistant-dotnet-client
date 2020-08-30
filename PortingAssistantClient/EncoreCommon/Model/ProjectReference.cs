using System;
using System.IO;
using System.Xml.Linq;
using EncoreCommon.Utils;

namespace EncoreCommon.Model
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
