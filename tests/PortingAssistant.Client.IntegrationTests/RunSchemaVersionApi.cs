using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using NUnit.Framework;

namespace PortingAssistant.Client.IntegrationTests
{
    class RunSchemaVersionApi
    {
        [Test]
        public void SchemaVersionApiShortOutputExpectedSchemaVersion()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(
               "PortingAssistant.Client.CLI.exe");
            startInfo.WorkingDirectory = TestContext.CurrentContext.TestDirectory;
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "schema -s";

            string version = "";
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    Console.WriteLine(exeProcess.StandardError.ReadToEnd());
                    version = exeProcess.StandardOutput.ReadToEnd();
                    exeProcess.WaitForExit();
                }
            }
            catch
            {
                Console.WriteLine("Fail to execute PA Client CLI!");
                Assert.Fail();
            }

            version = version.Split("\r\n")[0];
;           Assert.AreEqual("1.0", version);
        }

        [Test]
        public void SchemaVersionApiLongOutputExpectedSchemaVersion()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(
               "PortingAssistant.Client.CLI.exe");
            startInfo.WorkingDirectory = TestContext.CurrentContext.TestDirectory;
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "schema --schema-version";

            string version = "";
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    Console.WriteLine(exeProcess.StandardError.ReadToEnd());
                    version = exeProcess.StandardOutput.ReadToEnd();
                    exeProcess.WaitForExit();
                }
            }
            catch
            {
                Console.WriteLine("Fail to execute PA Client CLI!");
                Assert.Fail();
            }
            version = version.Split("\r\n")[0];
;           Assert.AreEqual("1.0", version);
        }
    }
}
