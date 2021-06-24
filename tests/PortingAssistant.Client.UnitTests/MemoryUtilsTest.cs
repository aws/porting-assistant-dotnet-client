using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
        public void TestLogSolutiontSize()
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

            // Run explicit GC
            GC.Collect();

            for (int i = 0; i <= 10; i++)
            {
                // Before the execution
                long kbBeforeExecution = GC.GetTotalMemory(false) / 1024;

                var watch = Stopwatch.StartNew();
                MemoryUtils.LogSolutiontSize(testLogger, testSolutionPath);
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;

                long kbAfterExecution = GC.GetTotalMemory(false) / 1024;
                // This will force garbage collection
                long kbAfterGC = GC.GetTotalMemory(true) / 1024;

                Console.WriteLine("----------Iteration " + i + "----------");
                Console.WriteLine(elapsedMs + "ms to run LogSolutiontSize");
                Console.WriteLine(kbBeforeExecution + "kb before LogSolutionSize.");
                Console.WriteLine(kbAfterExecution + "kb after LogSolutionSize.");
                Console.WriteLine(kbAfterGC + "kb after Garbage Collection");
                Console.WriteLine(kbAfterExecution - kbBeforeExecution + "kb allocated during LogSolutionSize.");
                Console.WriteLine(kbAfterExecution - kbAfterGC + "kb got collected by GC.");

                // Verify All the short-lived objects in LogSolutiontSize are gabage collected
                Assert.GreaterOrEqual(kbBeforeExecution - kbAfterGC, 0);
            }
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Directory.Delete(tmpTestFixturePath, true);
        }
    }
}
