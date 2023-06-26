using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.IntegrationTests.TestUtils;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace PortingAssistant.Client.IntegrationTests
{
    class RunPortingWithWebFormFramework 
    {
        private string expectedPortedTestSolutionExtractionPath;
        private string actualTestSolutionExtractionPath;
        protected string testDirectoryRoot;
        protected string tmpTestFixturePath;
        protected string testProjectZipPath;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            testDirectoryRoot = TestContext.CurrentContext.TestDirectory;

            tmpTestFixturePath = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName()));
            Directory.CreateDirectory(tmpTestFixturePath);

            testProjectZipPath = Path.Combine(
                testDirectoryRoot,
                "TestProjects",
                "eShopOnBlazor.zip");

            // Extract the baseline ported project
            expectedPortedTestSolutionExtractionPath = Path.Combine(
                tmpTestFixturePath, "expected");
            Directory.CreateDirectory(
                expectedPortedTestSolutionExtractionPath);
            string expectedTestProjectZipPath = Path.Combine(
                testDirectoryRoot,
                "TestProjects",
                "eShopOnBlazor-ported.zip");
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
        public void RunPortingWithWebFormFrameowork()
        {
            RunCLIToPortSolution();
            string expectedPortedTestSolutionPath = Path.Combine(
                expectedPortedTestSolutionExtractionPath,
                "eShopOnBlazor-ported", "src\\eShopLegacyWebForms");
            string actualPortedTestSolutionPath = Path.Combine(
                actualTestSolutionExtractionPath,
                "eShopOnBlazor", "src\\eShopLegacyWebForms");
            string[] filesToIgnore = {
                //Ignore PortSolutionResult.txt and eShopLegacyWebForms.csproj
                // because it contains solution path which is different between
                // baseline ported solution and actual test solution
                "eShopLegacyWebForms.csproj",
                "PortSolutionResult.txt",
                "PortSolutionResult.json",
                "eShopLegacyWebForms\\bin",
                "eShopLegacyWebForms\\obj",
                ".suo",
                "applicationhost.config"
            };
            Assert.IsTrue(DirectoryUtils.AreTwoDirectoriesEqual(
                expectedPortedTestSolutionPath,
                actualPortedTestSolutionPath,
                filesToIgnore));
        }

        [Test]
        public void RunPortingGeneratorWithWebFormFramework()
        {
            RunCLIToPortSolutionWithGenerator();
            string expectedPortedTestSolutionPath = Path.Combine(
                expectedPortedTestSolutionExtractionPath,
                "eShopOnBlazor-ported", "src\\eShopLegacyWebForms");
            string actualPortedTestSolutionPath = Path.Combine(
                actualTestSolutionExtractionPath,
                "eShopOnBlazor", "src\\eShopLegacyWebForms");
            string[] filesToIgnore = {
                //Ignore PortSolutionResult.txt and eShopLegacyWebForms.csproj
                // because it contains solution path which is different between
                // baseline ported solution and actual test solution
                "eShopLegacyWebForms.csproj",
                "PortSolutionResult.txt",
                "PortSolutionResult.json",
                "eShopLegacyWebForms\\bin",
                "eShopLegacyWebForms\\obj",
                ".suo",
                "applicationhost.config"
            };
            Assert.IsTrue(DirectoryUtils.AreTwoDirectoriesEqual(
                expectedPortedTestSolutionPath,
                actualPortedTestSolutionPath,
                filesToIgnore));
        }

        [Test]
        public void UseInvalidCliOptions()
        {
            string actualTestSolutionPath = Path.Combine(
                actualTestSolutionExtractionPath,
                "eShopOnBlazor",
                "eShopOnBlazor.sln");
            string actualTestProjectPath = Path.Combine(
                actualTestSolutionExtractionPath,
                "eShopOnBlazor",
                "eShopOnBlazor",
                "src",
                "eShopLegacyWebForms",
                "eShopLegacyWebForms.csproj");
            string actualAnalysisResultRootDir = actualTestSolutionExtractionPath;


            ProcessStartInfo startInfo = new ProcessStartInfo(
                "PortingAssistant.Client.CLI.exe");
            startInfo.WorkingDirectory = testDirectoryRoot;
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            startInfo.Arguments = "assess -s" + " " + "-o " + actualAnalysisResultRootDir
                                  + " " + "-t " + "net6.0"
                                  + " " + "-p " + "eShopLegacyWebForms";

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();

                Assert.AreEqual(-1, exeProcess.ExitCode);
            }

            Console.WriteLine("Invalid solution path was correctly detected.");

            startInfo.Arguments = "assess -s " + actualTestSolutionPath
                                  + " " + "-o"
                                  + " " + "-t " + "net6.0"
                                  + " " + "-p " + "eShopLegacyWebForms";

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();

                Assert.AreEqual(-1, exeProcess.ExitCode);
            }

            Console.WriteLine("Invalid output path was correctly detected.");

            startInfo.Arguments = "assess -s " + actualTestSolutionPath
                                  + " " + "-o " + actualAnalysisResultRootDir
                                  + " " + "-t"
                                  + " " + "-p " + "eShopLegacyWebForms";

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();

                Assert.AreEqual(-1, exeProcess.ExitCode);
            }

            Console.WriteLine("Invalid target framework was correctly detected.");
        }

        private void RunCLIToPortSolution()
        {
            string actualTestSolutionPath = Path.Combine(
                actualTestSolutionExtractionPath,
                "eShopOnBlazor",
                "eShopOnBlazor.sln");
            string actualTestProjectPath = Path.Combine(
                actualTestSolutionExtractionPath,
                "eShopOnBlazor",
                "eShopOnBlazor",
                "src",
                "eShopLegacyWebForms",
                "eShopLegacyWebForms.csproj");
            string actualAnalysisResultRootDir = actualTestSolutionExtractionPath;


            ProcessStartInfo startInfo = new ProcessStartInfo(
                "PortingAssistant.Client.CLI.exe");
            startInfo.WorkingDirectory = testDirectoryRoot;
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "assess -s " + actualTestSolutionPath
                + " " + "-o " + actualAnalysisResultRootDir
                + " " + "-p " + "eShopLegacyWebForms";

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

        private void RunCLIToPortSolutionWithGenerator()
        {
            string actualTestSolutionPath = Path.Combine(
                actualTestSolutionExtractionPath,
                "eShopOnBlazor",
                "eShopOnBlazor.sln");
            string actualTestProjectPath = Path.Combine(
                actualTestSolutionExtractionPath,
                "eShopOnBlazor",
                "eShopOnBlazor",
                "src",
                "eShopLegacyWebForms",
                "eShopLegacyWebForms.csproj");
            string actualAnalysisResultRootDir = actualTestSolutionExtractionPath;


            ProcessStartInfo startInfo = new ProcessStartInfo(
                "PortingAssistant.Client.CLI.exe");
            startInfo.WorkingDirectory = testDirectoryRoot;
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "assess -s " + actualTestSolutionPath
                                               + " " + "-o " + actualAnalysisResultRootDir
                                               + " " + "-p " + "eShopLegacyWebForms"
                                               + " " + "-u";

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
    }
}
