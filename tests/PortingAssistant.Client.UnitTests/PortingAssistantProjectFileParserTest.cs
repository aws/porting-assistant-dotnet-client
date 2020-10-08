using System.IO;
using System.Linq;
using PortingAssistant.Client.Handler.FileParser;
using NUnit.Framework;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Tests
{
    public class PortingAssistantProjectFileParserTest
    {
        [Test]
        public void ParseProjectWithPackageConfigSucceeds()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "ProjectWithPackagesConfig", "ProjectWithPackagesConfig.csproj");
            var handler = new ProjectFileParser(path);

            Assert.AreEqual(8, handler.GetPackageReferences().Count);
            Assert.AreEqual(4, handler.GetProjectReferences().Count);
            Assert.AreEqual(1, handler.GetTargetFrameworks().Count);
            Assert.AreEqual("net451", handler.GetTargetFrameworks().First());
        }

        [Test]
        public void ParseProjectWithProjectReferencesSucceeds()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "ProjectWithReference", "ProjectWithReference.csproj");
            var handler = new ProjectFileParser(path);

            Assert.AreEqual(4, handler.GetPackageReferences().Count);
            Assert.AreEqual(1, handler.GetProjectReferences().Count);
            Assert.AreEqual(1, handler.GetTargetFrameworks().Count);
            Assert.AreEqual("netcoreapp3.1", handler.GetTargetFrameworks().First());
        }

        [Test]
        public void ParseProjectInWrongDirectoryThrowsException()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithFailedContent", "ProjectInWrongDirectory.csproj");
            Assert.Throws<PortingAssistantClientException>(() =>
            {
                var projectFileParser = new ProjectFileParser(path);
                projectFileParser.GetPackageReferences();
            });
        }

        [Test]
        public void ParseProjectWithCorruptPackageVersionThrowsException()
        {
            var projectReferencePath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithFailedContent", "test", "ProjectWithCorruptPackageVersion.csproj");

            var projectFileParser = new ProjectFileParser(projectReferencePath);
            Assert.AreEqual(1, projectFileParser.GetPackageReferences().Count);
        }

        [Test]
        public void ParseProjectWithCorruptContentThrowsException()
        {
            var corruptProjectFileParser = new ProjectFileParser(Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithFailedContent", "test", "ProjectWithCorruptContent.csproj"));
            Assert.AreEqual(0, corruptProjectFileParser.GetPackageReferences().Count);
        }
    }
}
