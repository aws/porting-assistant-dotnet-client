using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PortingAssistantExtensionTelemetry.Model;

namespace PortingAssistant.Client.IntegrationTests
{
    public class AssessOptionsTest
    {
        private string _tmpTestProjectsExtractionPath;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _tmpTestProjectsExtractionPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(_tmpTestProjectsExtractionPath);
            string testProjectsPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "TestNet31Empty.zip");

            using (ZipArchive archive = ZipFile.Open(testProjectsPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(_tmpTestProjectsExtractionPath);
            }
        }

        [Test]
        public void AssessOption_EgressPoint_WhenNotEmpty()
        {
            string actualTestSolutionPath = Path.Combine(
                _tmpTestProjectsExtractionPath,
                "TestNet31Empty",
                "TestNet31Empty.sln");
            var outputDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestNet31Empty-output");
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            string stdout = "";

            ProcessStartInfo startInfo = new ProcessStartInfo(
                "PortingAssistant.Client.CLI.exe");
            startInfo.WorkingDirectory = TestContext.CurrentContext.TestDirectory;
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "assess -s " + actualTestSolutionPath
                + " " + "-o " + outputDirectory
                + " " + "-e " + "https://8cvsix1u33.execute-api.us-east-1.amazonaws.com/gamma";

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    stdout = exeProcess.StandardOutput.ReadToEnd();
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
            Assert.IsTrue(stdout.Contains("Change endpoint to https://8cvsix1u33.execute-api.us-east-1.amazonaws.com/gamma"));
            Directory.Delete(outputDirectory, true);
        }
    }
}
