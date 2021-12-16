using System;
using System.IO;
using NUnit.Framework;
using PortingAssistantExtensionTelemetry.Model;
using PortingAssistant.Client.Telemetry;
using System.Collections.Generic;
using Moq;
using System.Net.Http;
using Moq.Protected;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using Newtonsoft.Json;

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

        [Test]
        public void File_Line_Map_Updated_On_Upload()
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
               })
               .Verifiable();

            // use real http client with mocked handler here
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://localhost"),
            };

            var profile = "default";
            var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logs = Path.Combine(roamingFolder, "Porting Assistant for .NET", "logs");
            var logFilePath = Path.Combine(logs, "portingAssistant-client-cli-test-2.log");
            var metricsFilePath = Path.Combine(logs, "portingAssistant-client-cli-test-2.metrics");

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
            };
            var lastReadTokenFile = Path.Combine(teleConfig.LogsPath, "lastToken.json");
            bool result = Uploader.Upload(teleConfig, httpClient, profile, "");
            Assert.IsTrue(result);
            var fileLineNumberMap = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(lastReadTokenFile));
            Assert.AreEqual(fileLineNumberMap[logFilePath], 3);
            Assert.AreEqual(fileLineNumberMap[metricsFilePath], 2);

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
    }

    
}