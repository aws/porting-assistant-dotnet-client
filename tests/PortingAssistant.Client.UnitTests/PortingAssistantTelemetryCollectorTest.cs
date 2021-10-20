using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Web.Helpers;
using NUnit.Framework;
using PortingAssistant.Client.Client.Utils;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionTelemetry;
using System.Security.Cryptography;
using PortingAssistantExtensionTelemetry.Model;

namespace PortingAssistant.Client.UnitTests
{
    public class TelemetryCollectorTests
    {

        [Test]
        public void CreateSolutionMetric_Returns_Expected_Metric()
        {
            var solutionPath = "C:/Users/CustomerName/nopCommerce/src/NopCommerce.sln";
            var encryptedSolutionPath = "462eb7f46af82bd5155ef9f28ca3f5f638f702a7423b105478fa3d9267a344da";
            var solutionName = "testSolution";
            var encryptedSolutionName = "204ca3d6a6e14bf11f8e0992afc0276d262585065fe37ab595a5cba5bbbcb766";
            var targetFramework = "netcoreapp3.1"; 
            string version = "testVersion";
            string source = "test_cli";
            string tag = "test";
            double analysisTime = 0;
            var date = DateTime.Now;
            var sha256hash = SHA256.Create();
            var solutionDetail = new SolutionDetails
            {
                SolutionName = solutionName,
                SolutionFilePath = solutionPath,
                ApplicationGuid = "test-application-guid",
                SolutionGuid = "test-solution-guid",
                RepositoryUrl = "https://github.com/test-project",
            };
            var actualSolutionMetric = TelemetryCollector.createSolutionMetric(solutionDetail, targetFramework, version, source, analysisTime, tag, sha256hash, date);
            Assert.AreEqual(actualSolutionMetric.solutionPath, encryptedSolutionPath);
            Assert.AreEqual(actualSolutionMetric.solutionName, encryptedSolutionName);
            Assert.AreEqual(actualSolutionMetric.ApplicationGuid, "test-application-guid");
            Assert.AreEqual(actualSolutionMetric.SolutionGuid, "test-solution-guid");
            Assert.AreEqual(actualSolutionMetric.RepositoryUrl, "https://github.com/test-project");
        }


        [Test]
        public void CreateProjectMetric_Returns_Expected_Metric()
        {
            var targetFramework = "netcoreapp3.1";
            var projectGuid = Guid.NewGuid().ToString();
            var projectName = "TestProject";
            var encryptedProjectName = "d1bedc36a331502eba246acba9b58c0b27912d9a8fe753b703f6ee5bbfd483c6";
            var projectDetails = new ProjectDetails
            {
                ProjectName = projectName,
                ProjectFilePath = "pathToFile",
                ProjectGuid = projectGuid,
                ProjectType = "FormatA",
                TargetFrameworks = new List<string> { "one", "two" },
                PackageReferences = new List<PackageVersionPair> { new PackageVersionPair { PackageId = "System.Diagnostics.Tools", Version = "4.1.2" }, new PackageVersionPair { PackageId = "", Version = "" } },
                ProjectReferences = new List<ProjectReference> { new ProjectReference { ReferencePath = "a" }, new ProjectReference { ReferencePath = "b" }, new ProjectReference { ReferencePath = "c" } },
                IsBuildFailed = false
            };
            var date = DateTime.Now;
            var sha256hash = SHA256.Create();
            string version = "testVersion";
            string source = "test_cli";
            string tag = "test";
            double analysisTime = 0;
            var actualProjectMetric = TelemetryCollector.createProjectMetric(projectDetails, targetFramework, version, source, analysisTime, tag, sha256hash, date);
            Assert.AreEqual(actualProjectMetric.projectGuid, projectGuid);
            Assert.AreEqual(actualProjectMetric.projectName, encryptedProjectName);
        }

        [Test]
        public void TelemetryConfig_Fields_Gets_Expected_Value()
        {

            var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logs = Path.Combine(roamingFolder, "Porting Assistant for .NET", "logs");
            var suffix = new List<string> { "log", "metric" };
            var teleConfig = new TelemetryConfiguration
            {
                InvokeUrl = "https://8q2itpfg51.execute-api.us-east-1.amazonaws.com/beta",
                Region = "us-east-1",
                LogsPath = logs,
                ServiceName = "appmodernization-beta",
                Description = "Test",
                LogFilePath = Path.Combine(logs, "portingAssistant-client-cli-test.log"),
                MetricsFilePath = Path.Combine(logs, "portingAssistant-client-cli-test.metrics"),
                Suffix = suffix,               
            };
            Assert.AreEqual(teleConfig.LogsPath, logs);
            Assert.AreEqual(teleConfig.Suffix, suffix);
        }

    }
}