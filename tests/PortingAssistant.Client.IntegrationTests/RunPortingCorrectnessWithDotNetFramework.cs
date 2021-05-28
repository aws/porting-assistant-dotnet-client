using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using NUnit.Framework;
using PortingAssistant.Client.IntegrationTests.TestUtils;

namespace PortingAssistant.Client.IntegrationTests
{
    public class RunPortingCorrectnessWithDotNetFrameworkTests
    {
        private string tmpTestFixturePath;
        private string expectedPortedTestSolutionExtractionPath;
        private string actualTestSolutionExtractionPath;


        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            tmpTestFixturePath = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName()));
            Directory.CreateDirectory(tmpTestFixturePath);

            // Extract the baseline ported project
            expectedPortedTestSolutionExtractionPath = Path.Combine(
                tmpTestFixturePath, "expected");
            Directory.CreateDirectory(
                expectedPortedTestSolutionExtractionPath);
            string expectedTestProjectZipPath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "TestProjects",
                "NetFrameworkExample-ported.zip");
            using (ZipArchive archive = ZipFile.Open(
                expectedTestProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(
                    expectedPortedTestSolutionExtractionPath);
            }

            // Extract the test project that will be ported during the test
            actualTestSolutionExtractionPath = Path.Combine(
                tmpTestFixturePath, "actual");
            Directory.CreateDirectory(actualTestSolutionExtractionPath);
            string actualTestProjectZipPath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "TestProjects",
                "NetFrameworkExample.zip");
            using (ZipArchive archive = ZipFile.Open(
                actualTestProjectZipPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(
                    actualTestSolutionExtractionPath);
            }
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Directory.Delete(tmpTestFixturePath, true);
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
                //Ignore PortSolutionResult.txt because it contains
                //solution path which is different between baseline
                //ported solution and actual test solution
                "PortSolutionResult.txt",
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

            ProcessStartInfo startInfo = new ProcessStartInfo(
                "PortingAssistant.Client.CLI.exe");
            startInfo.WorkingDirectory = TestContext.CurrentContext.TestDirectory;
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "-s " + actualTestSolutionPath
                + " " + "-o " + actualAnalysisResultRootDir
                + " " + "-p " + actualTestProjectPath;

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
    }
}