using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Web.Helpers;
using NUnit.Framework;
using PortingAssistant.Client.Client.Utils;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionTelemetry;
using PortingAssistantExtensionTelemetry.Model;
using System.Security.Cryptography;
using PortingAssistant.Client.Telemetry;

namespace PortingAssistant.Client.UnitTests
{
    public class UploaderTests
    {
        [Test]
        public void Upload_Returns_Success_Status() 
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
            string text = "Test line" + Environment.NewLine;
            File.WriteAllText(teleConfig.LogFilePath, text);
            File.WriteAllText(teleConfig.MetricsFilePath, text);
            var actualSuccessStatus = Uploader.Upload(teleConfig, client, profile, "");
            Assert.IsTrue(actualSuccessStatus);

        }

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

        [Test]
        public void Upload_Large_File_Returns_Success_Status()
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
                MetricsFilePath = Path.Combine(logs, "portingAssistant-client-cli-test.metrics")
        };
            string text = "Test line" + Environment.NewLine;
            File.WriteAllText(teleConfig.LogFilePath, text);
            File.WriteAllText(teleConfig.MetricsFilePath, text);
            string[] lines = new string[1000];
            for (int i = 0; i < 1000; i++) 
            {
                lines[i] = "test string";
            
            }
            File.AppendAllLines(teleConfig.LogFilePath, lines);
            File.AppendAllLines(teleConfig.MetricsFilePath, lines);
            var actualSuccessStatus = Uploader.Upload(teleConfig, client, profile, "");
            Assert.IsTrue(actualSuccessStatus);
        }
    }
}