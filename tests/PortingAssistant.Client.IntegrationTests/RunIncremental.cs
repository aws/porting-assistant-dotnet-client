using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace PortingAssistant.Client.IntegrationTests
{
    class RunIncremental
    {
        private IPortingAssistantClient portingAssistantClient;
        private string _tmpTestProjectsExtractionPath;
        private string _tmpSolutionDirectory;
        private Task<SolutionAnalysisResult> solutionAnalysisResultTask;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _tmpTestProjectsExtractionPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(_tmpTestProjectsExtractionPath);
            string testProjectsPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "NetFrameworkExample.zip");

            var config = new PortingAssistantConfiguration();
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, config);

            var services = serviceCollection.BuildServiceProvider();
            portingAssistantClient = services.GetService<IPortingAssistantClient>();

            using (ZipArchive archive = ZipFile.Open(testProjectsPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(_tmpTestProjectsExtractionPath);
            }

            var netFrameworkProjectPath = Path.Combine(_tmpTestProjectsExtractionPath, "NetFrameworkExample", "NetFrameworkExample.sln");
            solutionAnalysisResultTask = portingAssistantClient.AnalyzeSolutionAsync(netFrameworkProjectPath, new AnalyzerSettings() { TargetFramework = "netcoreapp3.1", ContiniousEnabled = true } );

            string solutionId;
            using (var sha = SHA256.Create())
            {
                byte[] textData = System.Text.Encoding.UTF8.GetBytes(netFrameworkProjectPath);
                byte[] hash = sha.ComputeHash(textData);
                solutionId = BitConverter.ToString(hash);
            }
            _tmpSolutionDirectory = Path.Combine(Path.GetTempPath(), solutionId);
            _tmpSolutionDirectory = _tmpSolutionDirectory.Replace("-", "");
        }

        static private void ConfigureServices(IServiceCollection serviceCollection, PortingAssistantConfiguration config)
        {
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
            serviceCollection.AddAssessment(config);
            serviceCollection.AddOptions();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Directory.Delete(_tmpTestProjectsExtractionPath, true);
            Directory.Delete(_tmpSolutionDirectory, true);
        }

        [Test]
        public void AnalyzeNetFrameworkProjectSucceeds()
        {
            Assert.DoesNotThrow(() => {
                solutionAnalysisResultTask.Wait();
            });
            Assert.AreEqual(0, solutionAnalysisResultTask.Result.FailedProjects.Count);
            Assert.Null(solutionAnalysisResultTask.Result.Errors);
        }

        [Test]
        public void CheckNugetPackageCache()
        {
            solutionAnalysisResultTask.Wait();
       
            var actualCachedPackages = Directory.GetFiles(_tmpSolutionDirectory);
            Assert.AreEqual(actualCachedPackages.Length, 17);
        }

        [Test]
        public void AnalyzeNetFrameworkSourceFileSucceds()
        {
            solutionAnalysisResultTask.Wait();

            var netFrameworkSolutionPath = Path.Combine(_tmpTestProjectsExtractionPath, "NetFrameworkExample", "NetFrameworkExample.sln");
            var projectPath = Path.Combine(_tmpTestProjectsExtractionPath, "NetFrameworkExample", "NetFrameworkExample", "NetFrameworkExample.csproj");
            var filePath = Path.Combine(_tmpTestProjectsExtractionPath, "NetFrameworkExample", "NetFrameworkExample", "Controllers", "HomeController.cs");

            var projectAnalysisResult = solutionAnalysisResultTask.Result.ProjectAnalysisResults.Find(p => p.ProjectFilePath == projectPath);
            var preportReferences = projectAnalysisResult.PreportMetaReferences;
            var metaReferences = projectAnalysisResult.MetaReferences;
            var externalReferences = projectAnalysisResult.ExternalReferences;
            var projectRules = projectAnalysisResult.ProjectRules;

            var fileAnalysisResultTask = portingAssistantClient.AnalyzeFileAsync(filePath, projectPath, netFrameworkSolutionPath,
                preportReferences, metaReferences, projectRules, externalReferences, new AnalyzerSettings {TargetFramework = "netcoreapp3.1" });

            Assert.DoesNotThrow(() =>
            {
                fileAnalysisResultTask.Wait();
            });

            var sourceFileAnalysisResults = fileAnalysisResultTask.Result;

            var homeController = sourceFileAnalysisResults.Find(s => s.SourceFileName == "HomeController.cs");
            Assert.AreEqual(Path.Combine(_tmpTestProjectsExtractionPath, "NetFrameworkExample",
                "NetFrameworkExample", "Controllers", "HomeController.cs"), homeController.SourceFilePath);

            var apiAnalysisResult = homeController.ApiAnalysisResults.Find(r => r.CodeEntityDetails.OriginalDefinition
                == "System.Web.Mvc.Controller.View()");

            Assert.AreEqual(CodeEntityType.Method, apiAnalysisResult.CodeEntityDetails.CodeEntityType);
            Assert.AreEqual("View", apiAnalysisResult.CodeEntityDetails.Name);
            Assert.AreEqual("System.Web.Mvc", apiAnalysisResult.CodeEntityDetails.Namespace);
            Assert.AreEqual("System.Web.Mvc.Controller.View()", apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual("System.Web.Mvc.Controller.View()", apiAnalysisResult.CodeEntityDetails.Signature);
            Assert.AreEqual("System.Web.Mvc", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("5.2.7", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual(PackageSourceType.NUGET, apiAnalysisResult.CodeEntityDetails.Package.PackageSourceType);
            Assert.AreEqual(Compatibility.INCOMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.NoRecommendation, apiAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(apiAnalysisResult.Recommendations.RecommendedActions.First().Description);
        }
    }
}
