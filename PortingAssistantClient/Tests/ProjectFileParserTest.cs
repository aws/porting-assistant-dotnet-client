using System.IO;
using System.Linq;
using PortingAssistantHandler.FileParser;
using NUnit.Framework;
using PortingAssistant.Model;

namespace Tests
{
    public class ProjectFileParserTest
    {
        [Test]
        public void TestProjectWtihPackageConfig()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "ProjectWithPackagesConfig", "project.csproj");
            var handler = new ProjectFileParser(path);
            Assert.AreEqual(8, handler.GetPackageReferences().Count);
            Assert.AreEqual(4, handler.GetProjectReferences().Count);
            Assert.AreEqual(1, handler.GetTargetFrameworks().Count);
            Assert.AreEqual("net451", handler.GetTargetFrameworks().First());
        }

        [Test]
        public void TestProjectWtihProjectReferences()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "ProjectWithReference", "project.csproj");
            var handler = new ProjectFileParser(path);
            Assert.AreEqual(4, handler.GetPackageReferences().Count);
            Assert.AreEqual(1, handler.GetProjectReferences().Count);
            Assert.AreEqual(1, handler.GetTargetFrameworks().Count);
            Assert.AreEqual("netcoreapp3.1", handler.GetTargetFrameworks().First());
        }

        [Test]
        public void TestProjectWithCorruptedFile()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithFailedContent", "Nop.Core.csproj");
            Assert.Throws<PortingAssistantClientException>(() =>
            {
                var handler = new ProjectFileParser(path);
                handler.GetPackageReferences();
            });

            var projectReferencePath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithFailedContent", "test", "project.csproj");

            var mockhandler = new ProjectFileParser(projectReferencePath);
            Assert.AreEqual(1, mockhandler.GetPackageReferences().Count);

            var handler = new ProjectFileParser(Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithFailedContent", "test", "corrupt.csproj"));
            Assert.AreEqual(0, handler.GetPackageReferences().Count);
        }
    }
}
