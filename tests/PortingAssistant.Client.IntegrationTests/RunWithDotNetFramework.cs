using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using PortingAssistant.Client.Handler;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.IntegrationTests
{
    public class RunWithDotNetFrameworkTests
    {
        private IPortingAssistantHandler portingAssistantHandler;
        private string _tmpTestProjectsExtractionPath;
        private Task<SolutionAnalysisResult> solutionAnalysisResultTask;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _tmpTestProjectsExtractionPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(_tmpTestProjectsExtractionPath);
            string testProjectsPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "NetFrameworkExample.zip");

            var config = "config.json";
            var serviceConfig = new ConfigurationBuilder()
                .AddJsonFile(config)
                .Build();
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, serviceConfig);

            var services = serviceCollection.BuildServiceProvider();
            portingAssistantHandler = services.GetService<IPortingAssistantHandler>();

            using (ZipArchive archive = ZipFile.Open(testProjectsPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(_tmpTestProjectsExtractionPath);
            }

            var netFrameworkProjectPath = Path.Combine(_tmpTestProjectsExtractionPath, "NetFrameworkExample", "NetFrameworkExample.sln");
            solutionAnalysisResultTask = portingAssistantHandler.AnalyzeSolutionAsync(netFrameworkProjectPath, new Settings());

        }

        static private void ConfigureServices(IServiceCollection serviceCollection, IConfiguration config)
        {
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
            serviceCollection.AddAssessment(config.GetSection("AnalyzerConfiguration"));
            serviceCollection.AddOptions();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Directory.Delete(_tmpTestProjectsExtractionPath, true);
        }

        [Test]
        public void AnalyzeNetFrameworkProjectSucceeds()
        {
            Assert.DoesNotThrow(() =>{
                solutionAnalysisResultTask.Wait();
            });
            Assert.AreEqual(0, solutionAnalysisResultTask.Result.FailedProjects.Count);
            Assert.Null(solutionAnalysisResultTask.Result.Errors);
        }

        [Test]
        public void CheckSolutionDetails()
        {
            solutionAnalysisResultTask.Wait();
            var solutionDetails = solutionAnalysisResultTask.Result.SolutionDetails;

            Assert.AreEqual(0, solutionDetails.FailedProjects.Count);
            Assert.AreEqual("NetFrameworkExample", solutionDetails.SolutionName);
            Assert.AreEqual(Path.Combine(_tmpTestProjectsExtractionPath, "NetFrameworkExample", 
                "NetFrameworkExample.sln"), solutionDetails.SolutionFilePath);
            Assert.AreEqual(1, solutionDetails.Projects.Count);
        }

        [Test]
        public void CheckProjectDetails()
        {
            solutionAnalysisResultTask.Wait();
            var projectDetails = solutionAnalysisResultTask.Result.SolutionDetails.Projects.First();

            Assert.AreEqual("NetFrameworkExample", projectDetails.ProjectName);
            Assert.AreEqual(Path.Combine(_tmpTestProjectsExtractionPath, "NetFrameworkExample",
                "NetFrameworkExample", "NetFrameworkExample.csproj"), projectDetails.ProjectFilePath);
            Assert.AreEqual("{669D6AA1-29D1-47ED-9489-796D989351BA}", projectDetails.ProjectGuid);
            Assert.AreEqual("KnownToBeMSBuildFormat", projectDetails.ProjectType);
            Assert.AreEqual("net48", projectDetails.TargetFrameworks.First());
            Assert.AreEqual(0, projectDetails.ProjectReferences.Count);
            Assert.AreEqual(14, projectDetails.PackageReferences.Count);
        }

        [Test]
        public void CheckPackageAnalysisResult()
        {
            solutionAnalysisResultTask.Wait();
            var packageAnalysisResults = solutionAnalysisResultTask.Result.ProjectAnalysisResults.First().PackageAnalysisResults;
            Task.WaitAll(packageAnalysisResults.Values.ToArray());

            var packageAnalysisResult = packageAnalysisResults.GetValueOrDefault(new PackageVersionPair
            {
                PackageId = "bootstrap",
                Version = "3.4.1"
            }).Result;
            Assert.AreEqual(Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.
            GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.True(packageAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count > 0);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.AreEqual("4.0.0", packageAnalysisResult.Recommendations.RecommendedActions.First().Description);

            packageAnalysisResult = packageAnalysisResults.GetValueOrDefault(new PackageVersionPair
            {
                PackageId = "Microsoft.CodeDom.Providers.DotNetCompilerPlatform",
                Version = "2.0.1"
            }).Result;
            Assert.AreEqual(Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.
            GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, packageAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(packageAnalysisResult.Recommendations.RecommendedActions.First().Description);

            packageAnalysisResult = packageAnalysisResults.GetValueOrDefault(new PackageVersionPair
            {
                PackageId = "Newtonsoft.Json",
                Version = "12.0.2"
            }).Result;
            Assert.AreEqual(Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.
            GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.True(packageAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count > 0);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.AreEqual("12.0.3", packageAnalysisResult.Recommendations.RecommendedActions.First().Description);

            packageAnalysisResult = packageAnalysisResults.GetValueOrDefault(new PackageVersionPair
            {
                PackageId = "Antlr",
                Version = "3.5.0.2"
            }).Result;
            Assert.AreEqual(Compatibility.INCOMPATIBLE, packageAnalysisResult.CompatibilityResults.
            GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, packageAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(packageAnalysisResult.Recommendations.RecommendedActions.First().Description);

            packageAnalysisResult = packageAnalysisResults.GetValueOrDefault(new PackageVersionPair
            {
                PackageId = "Microsoft.AspNet.Mvc",
                Version = "5.2.7"
            }).Result;
            Assert.AreEqual(Compatibility.INCOMPATIBLE, packageAnalysisResult.CompatibilityResults.
            GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, packageAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(packageAnalysisResult.Recommendations.RecommendedActions.First().Description);
        }

        [Test]
        public void CheckApiAnalysisResult()
        {
            solutionAnalysisResultTask.Wait();
            var sourceFileAnalysisResults = solutionAnalysisResultTask.Result.ProjectAnalysisResults.First().SourceFileAnalysisResults;
            var bundlerConfigFile = sourceFileAnalysisResults.Find(s => s.SourceFileName == "BundleConfig.cs");

            Assert.AreEqual(Path.Combine(_tmpTestProjectsExtractionPath, "NetFrameworkExample",
                "NetFrameworkExample", "App_Start", "BundleConfig.cs"), bundlerConfigFile.SourceFilePath);
            var apiAnalysisResult = bundlerConfigFile.ApiAnalysisResults.Find(r => r.CodeEntityDetails.OriginalDefinition
                == "System.Web.Optimization.BundleCollection.Add(System.Web.Optimization.Bundle)");
            Assert.AreEqual(CodeEntityType.Namespace, apiAnalysisResult.CodeEntityDetails.CodeEntityType);
            Assert.AreEqual("Add", apiAnalysisResult.CodeEntityDetails.Name);
            Assert.AreEqual("System.Web.Optimization", apiAnalysisResult.CodeEntityDetails.Namespace);
            Assert.AreEqual("System.Web.Optimization.BundleCollection.Add(System.Web.Optimization.Bundle)",
                apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual("System.Web.Optimization.BundleCollection.Add(System.Web.Optimization.Bundle)", apiAnalysisResult.CodeEntityDetails.Signature);
            Assert.AreEqual("System.Web.Optimization", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("1.1.0", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual(PackageSourceType.NUGET, apiAnalysisResult.CodeEntityDetails.Package.PackageSourceType);
            Assert.AreEqual(Compatibility.UNKNOWN, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.NoRecommendation, apiAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(apiAnalysisResult.Recommendations.RecommendedActions.First().Description);

            apiAnalysisResult = bundlerConfigFile.ApiAnalysisResults.Find(r => r.CodeEntityDetails.OriginalDefinition
                == "System.Web.Optimization.Bundle.Include(string, params System.Web.Optimization.IItemTransform[])");
            Assert.AreEqual(CodeEntityType.Namespace, apiAnalysisResult.CodeEntityDetails.CodeEntityType);
            Assert.AreEqual("Include", apiAnalysisResult.CodeEntityDetails.Name);
            Assert.AreEqual("System.Web.Optimization", apiAnalysisResult.CodeEntityDetails.Namespace);
            Assert.AreEqual("System.Web.Optimization.Bundle.Include(string, params System.Web.Optimization.IItemTransform[])",
                apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual("System.Web.Optimization.Bundle.Include(string, params System.Web.Optimization.IItemTransform[])", apiAnalysisResult.CodeEntityDetails.Signature);
            Assert.AreEqual("System.Web.Optimization", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("1.1.0", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual(PackageSourceType.NUGET, apiAnalysisResult.CodeEntityDetails.Package.PackageSourceType);
            Assert.AreEqual(Compatibility.UNKNOWN, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.NoRecommendation, apiAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(apiAnalysisResult.Recommendations.RecommendedActions.First().Description);


            var routeConfigFile = sourceFileAnalysisResults.Find(s => s.SourceFileName == "RouteConfig.cs");
            Assert.AreEqual(Path.Combine(_tmpTestProjectsExtractionPath, "NetFrameworkExample",
                "NetFrameworkExample", "App_Start", "RouteConfig.cs"), routeConfigFile.SourceFilePath);

            apiAnalysisResult = routeConfigFile.ApiAnalysisResults.Find(r => r.CodeEntityDetails.OriginalDefinition
                == "System.Web.Routing.RouteCollection.IgnoreRoute(string)");
            Assert.AreEqual(CodeEntityType.Namespace, apiAnalysisResult.CodeEntityDetails.CodeEntityType);
            Assert.AreEqual("IgnoreRoute", apiAnalysisResult.CodeEntityDetails.Name);
            Assert.AreEqual("System.Web.Mvc", apiAnalysisResult.CodeEntityDetails.Namespace);
            Assert.AreEqual("System.Web.Routing.RouteCollection.IgnoreRoute(string)",
                apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual("System.Web.Routing.RouteCollection.IgnoreRoute(string)", apiAnalysisResult.CodeEntityDetails.Signature);
            Assert.AreEqual("System.Web.Mvc", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("5.2.7", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual(PackageSourceType.NUGET, apiAnalysisResult.CodeEntityDetails.Package.PackageSourceType);
            Assert.AreEqual(Compatibility.INCOMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.NoRecommendation, apiAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(apiAnalysisResult.Recommendations.RecommendedActions.First().Description);

            apiAnalysisResult = routeConfigFile.ApiAnalysisResults.Find(r => r.CodeEntityDetails.OriginalDefinition
                == "System.Web.Routing.RouteCollection.MapRoute(string, string, object)");
            Assert.AreEqual(CodeEntityType.Namespace, apiAnalysisResult.CodeEntityDetails.CodeEntityType);
            Assert.AreEqual("MapRoute", apiAnalysisResult.CodeEntityDetails.Name);
            Assert.AreEqual("System.Web.Mvc", apiAnalysisResult.CodeEntityDetails.Namespace);
            Assert.AreEqual("System.Web.Routing.RouteCollection.MapRoute(string, string, object)",
                apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual("System.Web.Routing.RouteCollection.MapRoute(string, string, object)", apiAnalysisResult.CodeEntityDetails.Signature);
            Assert.AreEqual("System.Web.Mvc", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("5.2.7", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual(PackageSourceType.NUGET, apiAnalysisResult.CodeEntityDetails.Package.PackageSourceType);
            Assert.AreEqual(Compatibility.INCOMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.NoRecommendation, apiAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(apiAnalysisResult.Recommendations.RecommendedActions.First().Description);

            var homeController = sourceFileAnalysisResults.Find(s => s.SourceFileName == "HomeController.cs");
            Assert.AreEqual(Path.Combine(_tmpTestProjectsExtractionPath, "NetFrameworkExample",
                "NetFrameworkExample", "Controllers", "HomeController.cs"), homeController.SourceFilePath);

            apiAnalysisResult = homeController.ApiAnalysisResults.Find(r => r.CodeEntityDetails.OriginalDefinition
                == "System.Web.Mvc.Controller.View()");
            Assert.AreEqual(CodeEntityType.Namespace, apiAnalysisResult.CodeEntityDetails.CodeEntityType);
            Assert.AreEqual("View", apiAnalysisResult.CodeEntityDetails.Name);
            Assert.AreEqual("System.Web.Mvc", apiAnalysisResult.CodeEntityDetails.Namespace);
            Assert.AreEqual("System.Web.Mvc.Controller.View()",apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
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