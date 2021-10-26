using System;
using System.IO;
using NUnit.Framework;
using PortingAssistantExtensionTelemetry.Model;
using PortingAssistant.Client.Telemetry;

namespace PortingAssistant.Client.UnitTests
{
    public class UploaderTests
    {

        [Test]
        public void Upload_Empty_File_Returns_Success_Status()
        {
            var client = new System.Net.Http.HttpClient();
            var profile = "default";
            var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logs = Path.Combine(roamingFolder, "Porting Assistant for .NET", "logs");
            var teleConfig = new TelemetryConfiguration
            {
                InvokeUrl = "https://8q2itpfg51.execute-api.us-east-1.amazonaws.com/beta",
                Region = "us-east-1",
                LogsPath = logs,
                ServiceName = "appmodernization-beta",
                Description = "Test",
                LogFilePath = Path.Combine(logs, "portingAssistant-client-cli-test.log"),
                MetricsFilePath = Path.Combine(logs, "portingAssistant-client-cli-test.metrics"),
            };

            var actualSuccessStatus = Uploader.Upload(teleConfig, client, profile, "");
            Assert.IsTrue(actualSuccessStatus);
        }

    }
}