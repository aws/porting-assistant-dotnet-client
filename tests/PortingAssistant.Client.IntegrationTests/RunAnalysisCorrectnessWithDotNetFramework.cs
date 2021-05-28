using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using NUnit.Framework;
using PortingAssistant.Client.IntegrationTests.TestUtils;

namespace PortingAssistant.Client.IntegrationTests
{
    public class RunAnalysisCorrectnessWithDotNetFrameworkTests
    {
        private string tmpTestProjectsExtractionPath;
        private string testSolutionPath;
        private string expectedAnalysisResultRootDir;
        private string actualAnalysisResultRootDir;


        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            expectedAnalysisResultRootDir = Path.Combine(
                TestContext.CurrentContext.TestDirectory, "TestProjects");

            tmpTestProjectsExtractionPath = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName()));
            Directory.CreateDirectory(tmpTestProjectsExtractionPath);
            string testProjectZipPath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "TestProjects",
                "NetFrameworkExample.zip");
            using (ZipArchive archive = ZipFile.Open(
                testProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tmpTestProjectsExtractionPath);
            }

            actualAnalysisResultRootDir = tmpTestProjectsExtractionPath;
            testSolutionPath = Path.Combine(
                tmpTestProjectsExtractionPath,
                "NetFrameworkExample",
                "NetFrameworkExample.sln");
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Directory.Delete(tmpTestProjectsExtractionPath, true);
        }

        [Test]
        public void FrameworkProjectAnalysisProduceExpectedJsonResult()
        {
            RunCLI();
            Assert.IsTrue(Directory.Exists(Path.Combine(
                actualAnalysisResultRootDir, "NetFrameworkExample-analyze")));
            CompareAnalysisResult();
        }

        private void RunCLI()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(
                "PortingAssistant.Client.CLI.exe");
            startInfo.WorkingDirectory = TestContext.CurrentContext.TestDirectory;
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "-s " + testSolutionPath + " -o " + actualAnalysisResultRootDir;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    string output = exeProcess.StandardOutput.ReadToEnd();
                    Console.WriteLine(output);
                    exeProcess.WaitForExit();
                }
            }
            catch
            {
                Console.WriteLine("Fail to execute PA Client CLI!");
                Assert.Fail();
            }
        }

        private void CompareAnalysisResult()
        {
            string expectedPackageAnalysisPath = Path.Combine(
                expectedAnalysisResultRootDir,
                "NetFrameworkExample-analyze",
                "NetFrameworkExample-package-analysis.json");
            string actualPackageAnalysisPath = Path.Combine(
                actualAnalysisResultRootDir,
                "NetFrameworkExample-analyze",
                "solution-analyze",
                "NetFrameworkExample",
                "NetFrameworkExample-package-analysis.json");
            string[] propertiesToBeRemovedInPackageAnalysisResult = { };
            Assert.IsTrue(JsonUtils.AreTwoJsonFilesEqual(
                expectedPackageAnalysisPath, actualPackageAnalysisPath, 
                propertiesToBeRemovedInPackageAnalysisResult));

            string expectedApiAnalysisPath = Path.Combine(
                expectedAnalysisResultRootDir,
                "NetFrameworkExample-analyze",
                "NetFrameworkExample-api-analysis.json");
            string actualApiAnalysisPath = Path.Combine(
                actualAnalysisResultRootDir,
                "NetFrameworkExample-analyze",
                "solution-analyze",
                "NetFrameworkExample",
                "NetFrameworkExample-api-analysis.json");
            string[] propertiesToBeRemovedInApiAnalysisResult = { "SourceFilePath", "Path" };
            Assert.IsTrue(JsonUtils.AreTwoJsonFilesEqual(
                expectedApiAnalysisPath, actualApiAnalysisPath, 
                propertiesToBeRemovedInApiAnalysisResult));
        }
    }
}