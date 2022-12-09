using System;
using System.IO;
using NUnit.Framework;
using PortingAssistantExtensionTelemetry.Model;
using PortingAssistant.Client.Telemetry;
using System.Collections.Generic;
using System.Linq;
using Moq;
using System.Net;
using Newtonsoft.Json;
using Amazon.Runtime;
using NUnit.Framework.Internal;
using Serilog;

namespace PortingAssistant.Client.UnitTests
{
    public class UploaderTests
    {
        // Temporarily remove this test as it requires AWS creds to be setup on the codebuild 
        [Test]
        [Ignore("Requires AWS profile configured in the environment")]
        public void Upload_Empty_File_Returns_Success_Status()
        {
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
                Suffix = new List<string> { ".log", ".metrics" }
            };
            bool actualSuccessStatus = false;
            if (TelemetryClientFactory.TryGetClient(profile, teleConfig, out ITelemetryClient client))
            {
                actualSuccessStatus = new Uploader(teleConfig, client, null, true).Run();
            }
            Assert.IsTrue(actualSuccessStatus);
        }

        [Test]
        public void Upload_Empty_File_Default_Creds_Returns_Success_Status()
        {
            var profile = "DEFAULT";
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
                Suffix = new List<string> { ".log", ".metrics" }
            };
            bool actualSuccessStatus = false;
            if (TelemetryClientFactory.TryGetClient(profile, teleConfig, out ITelemetryClient client, true))
            {
                actualSuccessStatus = new Uploader(teleConfig, client, null, true).Run();
            }
            Assert.IsTrue(actualSuccessStatus);
        }

        [Test]
        public void File_Line_Map_Updated_On_Upload()
        {
            var telemetryClientMock = new Mock<ITelemetryClient>();


            telemetryClientMock
                .Setup(
                    x => x.SendAsync(It.IsAny<TelemetryRequest>()).Result
                )
                .Returns(new AmazonWebServiceResponse()
                {
                    HttpStatusCode = HttpStatusCode.OK,
                })
                .Verifiable();

            var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logs = Path.Combine(roamingFolder, "Porting Assistant for .NET", "logs");
            var logFilePath = Path.Combine(logs, "portingAssistant-client-cli-test-2.log");
            var metricsFilePath = Path.Combine(logs, "portingAssistant-client-cli-test-2.metrics");

            if (!Directory.Exists(logs))
            {
                DirectoryInfo di = Directory.CreateDirectory(logs);
            }

            File.WriteAllLines(logFilePath, logLines);
            File.WriteAllLines(metricsFilePath, metricLogLines);

            var teleConfig = new TelemetryConfiguration
            {
                InvokeUrl = "https://localhost",
                Region = "us-east-1",
                LogsPath = logs,
                ServiceName = "appmodernization-beta",
                Description = "Test",
                LogFilePath = logFilePath,
                MetricsFilePath = metricsFilePath,
                Suffix = new List<string> { ".log", ".metrics" }
            };
            var lastReadTokenFile = Path.Combine(teleConfig.LogsPath, "lastToken.json");
            bool result = new Uploader(teleConfig, telemetryClientMock.Object, null, true).Run();
            Assert.IsTrue(result);
            var fileLineNumberMap = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(lastReadTokenFile));
            Assert.AreEqual(fileLineNumberMap[logFilePath], 3);
            Assert.AreEqual(fileLineNumberMap[metricsFilePath], 2);

            // cleanup lastToken.json 
            fileLineNumberMap.Remove(logFilePath);
            fileLineNumberMap.Remove(metricsFilePath);
            string jsonStringEmpty = JsonConvert.SerializeObject(fileLineNumberMap);
            File.WriteAllText(lastReadTokenFile, jsonStringEmpty);

            // write an invalid lastToken.json file
            var lastReadTokenLines = new Dictionary<String, string>();
            lastReadTokenLines.Add("A", "/a  ");
            lastReadTokenLines.Add("B", "\b");
            lastReadTokenLines.Add("C", null);
            string lastReadTokenLinesJson = JsonConvert.SerializeObject(lastReadTokenLines);
            File.WriteAllText(lastReadTokenFile, lastReadTokenLinesJson);

            // re-run Uploader, it should overwrite the invalid lastToken.json file.
            bool resultNew = new Uploader(teleConfig, telemetryClientMock.Object, null, true).Run();
            Assert.IsTrue(resultNew);
            Assert.AreEqual(fileLineNumberMap.ContainsKey("a"), false);

            //cleanup 
            File.Delete(logFilePath);
            File.Delete(metricsFilePath);
            fileLineNumberMap.Remove(logFilePath);
            fileLineNumberMap.Remove(metricsFilePath);
            string jsonString = JsonConvert.SerializeObject(fileLineNumberMap);
            File.WriteAllText(lastReadTokenFile, jsonString);
        }

        private List<string> metricLogLines = new List<string>()
        {
            "{\"solutionName\":\"9c904f9063f5fa3859ba411544a63d4d1889cc80821a5385952e504d53034554\",\"solutionPath\":\"8dfa8bd5900b241995c88da01d69b0a9e17f6b7f18c90bb645869ab71b29f5fa\",\"ApplicationGuid\":\"8602089b-96fd-4fa4-9b4d-36067c03e572\",\"SolutionGuid\":\"8602089b-96fd-4fa4-9b4d-36067c03e572\",\"RepositoryUrl\":null,\"analysisTime\":14315.3334,\"metricsType\":\"solution\",\"portingAssistantSource\":\"Porting Assistant Client CLI\",\"tag\":\"client\",\"version\":\"1.8.0\",\"targetFramework\":\"netcoreapp3.1\",\"timeStamp\":\"12/08/2021 11:52\"}",
            "{\"numNugets\":14,\"numReferences\":0,\"projectGuid\":\"669d6aa1-29d1-47ed-9489-796d989351ba\",\"isBuildFailed\":false,\"projectType\":\"KnownToBeMSBuildFormat\",\"projectName\":\"9c904f9063f5fa3859ba411544a63d4d1889cc80821a5385952e504d53034554\",\"sourceFrameworks\":[\"net48\"],\"metricsType\":\"project\",\"portingAssistantSource\":\"Porting Assistant Client CLI\",\"tag\":\"client\",\"version\":\"1.8.0\",\"targetFramework\":\"netcoreapp3.1\",\"timeStamp\":\"12/08/2021 11:52\"}"
        };

        private List<string> logLines = new List<string>()
        {
            "[2021 - 12 - 08 11:52:02 INF] (Porting Assistant Client CLI) (1.11.19 - alpha + a0d7b74f85) (client)PortingAssistant.Client.Analysis.PortingAssistantAnalysisHandler: Operating system is 64bit.",
            "[2021 - 12 - 08 11:52:02 INF](Porting Assistant Client CLI)(1.11.19 - alpha + a0d7b74f85)(client) PortingAssistant.Client.Analysis.PortingAssistantAnalysisHandler: Current process is 64bit.",
            "[2021 - 12 - 08 11:52:02 INF](Porting Assistant Client CLI)(1.11.19 - alpha + a0d7b74f85)(client) PortingAssistant.Client.Analysis.PortingAssistantAnalysisHandler: Total size for C:\\Users\\longachr\\AppData\\Local\\Temp\\u03lvutv.1mn\\NetFrameworkExample\\NetFrameworkExample.sln in bytes: 4673"
        };

        [Test]
        public void TestBlankProfilePassedIntoFactory()
        {
            var teleConfig = new TelemetryConfiguration();
            bool actualSuccessStatus = TelemetryClientFactory.TryGetClient("", teleConfig, out ITelemetryClient client);
            Assert.IsFalse(actualSuccessStatus);
            Assert.IsNull(client);
        }

        [Test]
        public void TestNullProfilePassedIntoFactory()
        {
            var teleConfig = new TelemetryConfiguration();
            bool actualSuccessStatus = TelemetryClientFactory.TryGetClient(null, teleConfig, out ITelemetryClient client);
            Assert.IsFalse(actualSuccessStatus);
            Assert.IsNull(client);
        }
    }
}