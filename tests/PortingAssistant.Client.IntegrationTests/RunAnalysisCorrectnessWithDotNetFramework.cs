using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using NUnit.Framework;
using PortingAssistant.Client.IntegrationTests.TestUtils;

namespace PortingAssistant.Client.IntegrationTests
{
    public class RunAnalysisCorrectnessWithDotNetFrameworkTests : CorrectnessTestBase
    {
        private string testSolutionPath;
        private string expectedAnalysisResultRootDir;
        private string actualAnalysisResultRootDir;


        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            expectedAnalysisResultRootDir = Path.Combine(
                testDirectoryRoot, "TestProjects");

            using (ZipArchive archive = ZipFile.Open(
                testProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tmpTestFixturePath);
            }

            actualAnalysisResultRootDir = tmpTestFixturePath;
            testSolutionPath = Path.Combine(
                tmpTestFixturePath,
                "NetFrameworkExample",
                "NetFrameworkExample.sln");
        }

        [Test]
        public void FrameworkProjectAnalysisProduceExpectedJsonResult()
        {
            RunCLIToAnalyzeSolution();
            Assert.IsTrue(Directory.Exists(Path.Combine(
                actualAnalysisResultRootDir, "NetFrameworkExample-analyze")));
            ValidateSchemas();
            CompareAnalysisResult();
        }

        private void RunCLIToAnalyzeSolution()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(
                "PortingAssistant.Client.CLI.exe");
            startInfo.WorkingDirectory = testDirectoryRoot;
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "assess -s " + testSolutionPath
                + " " + "-o " + actualAnalysisResultRootDir;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    string stdout = exeProcess.StandardOutput.ReadToEnd();
                    string stderr = exeProcess.StandardError.ReadToEnd();
                    //Console.WriteLine(stdout);
                    //Console.WriteLine(stderr);
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
            bool comparisonResult = JsonUtils.AreTwoJsonFilesEqual(
                expectedApiAnalysisPath, actualApiAnalysisPath,
                propertiesToBeRemovedInApiAnalysisResult);
            Assert.IsTrue(comparisonResult);
        }

        private void ValidateSchemas()
        {
            string actualPackageAnalysisPath = Path.Combine(
                actualAnalysisResultRootDir,
                "NetFrameworkExample-analyze",
                "solution-analyze",
                "NetFrameworkExample",
                "NetFrameworkExample-package-analysis.json");
            string packageAnalysisSchemaPath = Path.Combine(
               expectedAnalysisResultRootDir,
               "Schemas",
               "package-analysis-schema.json");
            Assert.IsTrue(JsonUtils.ValidateSchema(actualPackageAnalysisPath, packageAnalysisSchemaPath, true));

            string actualApiAnalysisPath = Path.Combine(
                actualAnalysisResultRootDir,
                "NetFrameworkExample-analyze",
                "solution-analyze",
                "NetFrameworkExample",
                "NetFrameworkExample-api-analysis.json");
            string apiAnalysisSchemaPath = Path.Combine(
               expectedAnalysisResultRootDir,
               "Schemas",
               "api-analysis-schema.json");
            Assert.IsTrue(JsonUtils.ValidateSchema(actualApiAnalysisPath, apiAnalysisSchemaPath, false));
        }
    }
}
