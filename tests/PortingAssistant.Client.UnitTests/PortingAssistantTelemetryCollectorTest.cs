using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionTelemetry;
using System.Security.Cryptography;
using PortingAssistantExtensionTelemetry.Model;
using Serilog;

namespace PortingAssistant.Client.UnitTests
{
    public class TelemetryCollectorTests
    {
        [Test]
        public void CreateSolutionMetric_Returns_Expected_Metric()
        {
            var solutionPath = "C:/Users/CustomerName/nopCommerce/src/NopCommerce.sln";
            // encryptedSolutionPath is the sha256 hash of the solutionPath string
            var encryptedSolutionPath = "462eb7f46af82bd5155ef9f28ca3f5f638f702a7423b105478fa3d9267a344da";
            var solutionName = "testSolution";
            // encryptedSolutionName is the sha256 hash of the solutionName string
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
                Projects = new List<ProjectDetails>()
            };
            solutionDetail.Projects.Add(new ProjectDetails() { LinesOfCode = 100 });
            var numLogicalCores = Environment.ProcessorCount;
            var actualSolutionMetric = TelemetryCollector.CreateSolutionMetric(solutionDetail, targetFramework, version, source, analysisTime, tag, sha256hash, date);
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
            string solutionPath = "test";
            string solutionGuid = "test";
            string encryptedSolutionPath = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";
            var actualProjectMetric = TelemetryCollector.CreateProjectMetric(projectDetails, targetFramework, version, source, analysisTime, tag, sha256hash, date, solutionPath, solutionGuid);
            Assert.AreEqual(actualProjectMetric.projectGuid, projectGuid);
            Assert.AreEqual(actualProjectMetric.projectName, encryptedProjectName);
            Assert.AreEqual(actualProjectMetric.solutionPath, encryptedSolutionPath);
            Assert.AreEqual(actualProjectMetric.SolutionGuid, solutionGuid);
        }

        [Test]
        public void TelemetryConfig_Fields_Gets_Expected_Value()
        {

            var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logs = Path.Combine(roamingFolder, "Porting Assistant for .NET", "logs");
            var suffix = new List<string> { "log", "metric" };
            var invokeUrl = "https://8q2itpfg51.execute-api.us-east-1.amazonaws.com/beta";
            var region = "us-east-1";
            var servicename = "appmodernization-beta";
            var description = "Test";
            var teleConfig = new TelemetryConfiguration
            {
                InvokeUrl = invokeUrl,
                Region = region,
                LogsPath = logs,
                ServiceName = servicename,
                Description = description,
                LogFilePath = Path.Combine(logs, "portingAssistant-client-cli-test.log"),
                MetricsFilePath = Path.Combine(logs, "portingAssistant-client-cli-test.metrics"),
                Suffix = suffix,
            };
            Assert.AreEqual(teleConfig.InvokeUrl, invokeUrl);
            Assert.AreEqual(teleConfig.Region, region);
            Assert.AreEqual(teleConfig.ServiceName, servicename);
            Assert.AreEqual(teleConfig.Description, description);
            Assert.AreEqual(teleConfig.LogsPath, logs);
            Assert.AreEqual(teleConfig.Suffix, suffix);
        }

        [Test]
        public void NullProject_Returns_EmptyProjectMetric()
        {
            ProjectDetails nullProjectDetails = null;

            TelemetryCollector.Builder(Log.Logger, Directory.GetCurrentDirectory());
            var projectMetric = TelemetryCollector.CreateProjectMetric(
                nullProjectDetails,
                string.Empty,
                string.Empty,
                string.Empty,
                int.MinValue,
                string.Empty,
                SHA256.Create(),
                DateTime.Now,
                string.Empty,
                string.Empty
            );

            Assert.AreEqual(projectMetric.numNugets, 0);
            Assert.AreEqual(projectMetric.numReferences, 0);
            Assert.AreEqual(projectMetric.linesOfCode, 0);
            Assert.IsTrue(string.IsNullOrWhiteSpace(projectMetric.projectGuid));
            Assert.IsTrue(string.IsNullOrWhiteSpace(projectMetric.projectType));
            Assert.IsTrue(string.IsNullOrWhiteSpace(projectMetric.projectName));
            Assert.IsTrue(string.IsNullOrWhiteSpace(projectMetric.language));
            Assert.IsTrue(string.IsNullOrWhiteSpace(projectMetric.solutionPath));
            Assert.IsTrue(string.IsNullOrWhiteSpace(projectMetric.SolutionGuid));
            Assert.IsFalse(projectMetric.isBuildFailed);
            Assert.IsNull(projectMetric.sourceFrameworks);
        }

        [Test]
        public void NullProject_Returns_EmptySolutionMetric()
        {
            SolutionDetails solutionDetails = null;
            TelemetryCollector.Builder(Log.Logger, Directory.GetCurrentDirectory());
            var solutionMetric = TelemetryCollector.CreateSolutionMetric(
                solutionDetails,
                string.Empty,
                string.Empty,
                string.Empty,
                int.MinValue,
                string.Empty,
                SHA256.Create(),
                DateTime.Now
            );

            Assert.AreEqual(solutionMetric.analysisTime, 0);
            Assert.AreEqual(solutionMetric.linesOfCode, 0);
            Assert.AreEqual(solutionMetric.numLogicalCores, 0);
            Assert.AreEqual(solutionMetric.systemMemory, 0);
            Assert.IsTrue(string.IsNullOrWhiteSpace(solutionMetric.solutionName));
            Assert.IsTrue(string.IsNullOrWhiteSpace(solutionMetric.ApplicationGuid));
            Assert.IsTrue(string.IsNullOrWhiteSpace(solutionMetric.RepositoryUrl));
            Assert.IsTrue(string.IsNullOrWhiteSpace(solutionMetric.solutionPath));
            Assert.IsTrue(string.IsNullOrWhiteSpace(solutionMetric.SolutionGuid));
        }

        [Test]
        public void VbProjectProject_Returns_VBLanguage()
        {
            var projectDetails = new ProjectDetails();
            projectDetails.ProjectFilePath = "any.vbproj";
            projectDetails.TargetFrameworks = new List<string>();
            projectDetails.ProjectGuid = string.Empty;
            projectDetails.ProjectName = string.Empty;
            projectDetails.ProjectType = string.Empty;
            projectDetails.PackageReferences = new List<PackageVersionPair>();
            projectDetails.ProjectReferences = new List<ProjectReference>();

            var projectMetric = TelemetryCollector.CreateProjectMetric(
                projectDetails,
                string.Empty,
                string.Empty,
                string.Empty,
                int.MinValue,
                string.Empty,
                SHA256.Create(),
                DateTime.Now,
                string.Empty,
                string.Empty
            );
            
            Assert.AreEqual(projectMetric.language, "visualbasic");
        }
    }
}
