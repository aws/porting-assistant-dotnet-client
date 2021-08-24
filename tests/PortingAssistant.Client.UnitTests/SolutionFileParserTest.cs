using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using PortingAssistant.Client.Client.FileParser;

namespace PortingAssistant.Client.UnitTests
{
    public class SolutionFileParserTest
    {
        protected string tmpTestFixturePath;
        protected string testSolutionPath;
        protected string testSolutionParentDir;

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            tmpTestFixturePath = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName()));
            Directory.CreateDirectory(tmpTestFixturePath);

            string testDirectoryRoot = TestContext.CurrentContext.TestDirectory;
            string testProjectZipPath = Path.Combine(
                testDirectoryRoot,
                "TestProjects",
                "mvcmusicstore.zip");

            using (ZipArchive archive = ZipFile.Open(
                testProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tmpTestFixturePath);
            }

            testSolutionParentDir = Path.Combine(tmpTestFixturePath, "mvcmusicstore");
            testSolutionPath = Path.Combine(testSolutionParentDir, "MvcMusicStore.sln");
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Directory.Delete(tmpTestFixturePath, true);
        }

        [Test]
        public void getSolutionGuid_Returns_Expected_Guid()
        {
            string actualSolutionGuid = SolutionFileParser.getSolutionGuid(testSolutionPath);
            string expectedSolutionGuid = "2ADD3674-EB3F-480D-BF17-3434E0BD5A5C".ToLower();
            Assert.AreEqual(expectedSolutionGuid, actualSolutionGuid);
        }

        [Test]
        public void getSolutionGuid_Returns_Null_On_NonExisting_Solution_Path()
        {
            string actualSolutionGuid = SolutionFileParser.getSolutionGuid(@"C:\\Random\\Path\\Invalid\\Solution.sln");
            Assert.AreEqual(null, actualSolutionGuid);
        }

        [Test]
        public void getSolutionGuid_Returns_Null_On_Invalid_Solution_Path()
        {
            string projectPath = Path.Combine(
                testSolutionParentDir,
                "MvcMusicStore", "MvcMusicStore.csproj"
                );
            // Path exists, but not a valid solution file with file extension .sln
            string actualSolutionGuid = SolutionFileParser.getSolutionGuid(projectPath);
            Assert.AreEqual(null, actualSolutionGuid);
        }
    }
}
