using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using PortingAssistant.Client.Common.Utils;

namespace PortingAssistant.Client.UnitTests
{
    class MemoryUtilsTest
    {
        private string testDirectoryRoot;
        private string tmpTestFixturePath;
        private ILogger testLogger;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            testDirectoryRoot = TestContext.CurrentContext.TestDirectory;
            tmpTestFixturePath = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName()));
            Directory.CreateDirectory(tmpTestFixturePath);

            // Extract test solution at tmpTestFixturePath
            string testProjectZipPath = Path.Combine(
                testDirectoryRoot,
                "TestProjects",
                "mvcmusicstore.zip");
            using (ZipArchive archive = ZipFile.Open(
                testProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tmpTestFixturePath);
            }

            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            var factory = serviceProvider.GetService<ILoggerFactory>();

            testLogger = factory.CreateLogger("TestLogger");
            testLogger.LogInformation("Total logs");
        }

        [Test]
        public void TestLogSolutionSize()
        {
            var testSolutionPath = Path.Combine(
                tmpTestFixturePath, "mvcmusicstore", "MvcMusicStore.sln");
            DirectoryInfo solutionDir = Directory.GetParent(testSolutionPath);
            var totalFileCount = solutionDir.EnumerateFiles(
                "*", SearchOption.AllDirectories).Count();
            Assert.AreEqual(totalFileCount, 299);
            var csFileCount = solutionDir.EnumerateFiles(
                "*.cs", SearchOption.AllDirectories).Count();
            Assert.AreEqual(csFileCount, 41);

            // NOTE: This size will change if mvcMusicStore is modified
            var expectedSize = 202736;
            var size = MemoryUtils.LogSolutionSize(testLogger, testSolutionPath);

            // Verify that the solution size is correct
            Assert.AreEqual(expectedSize, size);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Directory.Delete(tmpTestFixturePath, true);
        }

        [Test]
        public void TestLogSolutionSizeWithNullSolutionPath()
        {
            string nullSolutionPath = null;
            var size = MemoryUtils.LogSolutionSize(testLogger, nullSolutionPath);
            Assert.AreEqual(0, size);
        }
    }
}
