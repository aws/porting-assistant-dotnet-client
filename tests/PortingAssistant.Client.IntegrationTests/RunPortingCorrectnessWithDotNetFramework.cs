using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using NUnit.Framework;
using PortingAssistant.Client.IntegrationTests.TestUtils;

namespace PortingAssistant.Client.IntegrationTests
{
    public class RunPortingCorrectnessWithDotNetFrameworkTests : CorrectnessTestBase
    {
        private string expectedPortedTestSolutionExtractionPath;
        private string actualTestSolutionExtractionPath;


        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Extract the baseline ported project
            expectedPortedTestSolutionExtractionPath = Path.Combine(
                tmpTestFixturePath, "expected");
            Directory.CreateDirectory(
                expectedPortedTestSolutionExtractionPath);
            string expectedTestProjectZipPath = Path.Combine(
                testDirectoryRoot,
                "TestProjects",
                "NetFrameworkExample-ported.zip");
            using (ZipArchive archive = ZipFile.Open(
                expectedTestProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(
                    expectedPortedTestSolutionExtractionPath);
            }

            // Extract the test project that will be ported during the test
            actualTestSolutionExtractionPath = tmpTestFixturePath;
            Directory.CreateDirectory(actualTestSolutionExtractionPath);
            using (ZipArchive archive = ZipFile.Open(
                testProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(
                    actualTestSolutionExtractionPath);
            }
        }

        [Test]
        public void PortingFrameworkProjectProducesExpectedPortedProject()
        {
            RunCLIToPortSolution();
            string expectedPortedTestSolutionPath = Path.Combine(
                expectedPortedTestSolutionExtractionPath,
                "NetFrameworkExample");
            string actualPortedTestSolutionPath = Path.Combine(
                actualTestSolutionExtractionPath,
                "NetFrameworkExample");
            string[] filesToIgnore = {
                //Ignore PortSolutionResult.txt and NetFrameworkExample.csproj
                // because it contains solution path which is different between
                // baseline ported solution and actual test solution
                "NetFrameworkExample.csproj",
                "PortSolutionResult.txt",
                "PortSolutionResult.json",
                "NetFrameworkExample\\bin",
                "NetFrameworkExample\\obj"
            };
            Assert.IsTrue(DirectoryUtils.AreTwoDirectoriesEqual(
                expectedPortedTestSolutionPath,
                actualPortedTestSolutionPath,
                filesToIgnore));
        }

        private void RunCLIToPortSolution()
        {
            string actualTestSolutionPath = Path.Combine(
                actualTestSolutionExtractionPath,
                "NetFrameworkExample",
                "NetFrameworkExample.sln");
            string actualTestProjectPath = Path.Combine(
                actualTestSolutionExtractionPath,
                "NetFrameworkExample",
                "NetFrameworkExample",
                "NetFrameworkExample.csproj");
            string actualAnalysisResultRootDir = actualTestSolutionExtractionPath;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(Path.Combine(testDirectoryRoot, "PortingAssistant.Client.CLI.exe"),
                    $"-s {actualTestSolutionPath} -o {actualAnalysisResultRootDir} -p {actualTestProjectPath}"))
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
    }
}
