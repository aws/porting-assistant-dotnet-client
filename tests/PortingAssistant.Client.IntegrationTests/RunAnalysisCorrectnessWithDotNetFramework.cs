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
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(Path.Combine(testDirectoryRoot, "PortingAssistant.Client.CLI.exe"), $"assess -s {testSolutionPath} -o {actualAnalysisResultRootDir}"))
                {
                    exeProcess.WaitForExit(300000);
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
                propertiesToBeRemovedInPackageAnalysisResult), "Package analysis did not match expected");

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
            Assert.IsTrue(comparisonResult, "API analysis did not match expected");
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
            Assert.IsTrue(JsonUtils.ValidateSchema(actualPackageAnalysisPath, packageAnalysisSchemaPath, true), "Package analysis schema does not match expected");

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
            Assert.IsTrue(JsonUtils.ValidateSchema(actualApiAnalysisPath, apiAnalysisSchemaPath, false), "API analysis schema does not match expected");
        }
    }
}
