using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace PortingAssistant.Client.IntegrationTests
{
    class RunWithDotNetCoreFrameowrkTests
    {
        private IPortingAssistantClient portingAssistantClient;
        private string _tmpTestProjectsExtractionPath;
        private Task<SolutionAnalysisResult> solutionAnalysisResultTask;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _tmpTestProjectsExtractionPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(_tmpTestProjectsExtractionPath);
            string testProjectsPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "Miniblog.Core-master.zip");

            var config = new PortingAssistantConfiguration();
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, config);

            var services = serviceCollection.BuildServiceProvider();
            portingAssistantClient = services.GetService<IPortingAssistantClient>();

            using (ZipArchive archive = ZipFile.Open(testProjectsPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(_tmpTestProjectsExtractionPath);
            }

            var netCoreProjectPath = Path.Combine(_tmpTestProjectsExtractionPath, "Miniblog.Core-master", "Miniblog.Core.sln");
            solutionAnalysisResultTask = portingAssistantClient.AnalyzeSolutionAsync(netCoreProjectPath, new AnalyzerSettings());
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
        }

        [Test]
        public void AnalyzeNetCoreProjectSucceeds()
        {
            Assert.DoesNotThrow(() => {
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
            Assert.AreEqual("Miniblog.Core", solutionDetails.SolutionName);
            Assert.AreEqual(Path.Combine(_tmpTestProjectsExtractionPath, "Miniblog.Core-master",
                "Miniblog.Core.sln"), solutionDetails.SolutionFilePath);
            Assert.AreEqual(1, solutionDetails.Projects.Count);
        }

        [Test]
        public void CheckProjectDetails()
        {
            solutionAnalysisResultTask.Wait();
            var projectDetails = solutionAnalysisResultTask.Result.SolutionDetails.Projects.First();

            Assert.AreEqual("Miniblog.Core", projectDetails.ProjectName);
            Assert.AreEqual(Path.Combine(_tmpTestProjectsExtractionPath, "Miniblog.Core-master",
                "src", "Miniblog.Core.csproj"), projectDetails.ProjectFilePath);
            Assert.AreEqual("8CE1A353-8B0D-43F1-B681-6057BAC241BC".ToLower(), projectDetails.ProjectGuid);
            Assert.AreEqual("KnownToBeMSBuildFormat", projectDetails.ProjectType);
            Assert.AreEqual("netcoreapp3.1", projectDetails.TargetFrameworks.First());
            Assert.AreEqual(0, projectDetails.ProjectReferences.Count);
            Assert.AreEqual(13, projectDetails.PackageReferences.Count);
        }

        [Test]
        public void CheckPackageAnalysisResult()
        {
            solutionAnalysisResultTask.Wait();
            var packageAnalysisResults = solutionAnalysisResultTask.Result.ProjectAnalysisResults.First().PackageAnalysisResults;
            Task.WaitAll(packageAnalysisResults.Values.ToArray());

            var packageAnalysisResult = packageAnalysisResults.GetValueOrDefault(new PackageVersionPair
            {
                PackageId = "Azure.ImageOptimizer",
                Version = "1.1.0.39",
                PackageSourceType = PackageSourceType.NUGET
            }).Result;
            Assert.AreEqual(Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.
            GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, packageAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(packageAnalysisResult.Recommendations.RecommendedActions.First().Description);

            packageAnalysisResult = packageAnalysisResults.GetValueOrDefault(new PackageVersionPair
            {
                PackageId = "LigerShark.WebOptimizer.Sass",
                Version = "3.0.40-beta",
                PackageSourceType = PackageSourceType.NUGET
            }).Result;
            Assert.AreEqual(Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.
            GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.True(packageAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count > 0);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.AreEqual("3.0.42-beta", packageAnalysisResult.Recommendations.RecommendedActions.First().Description);

            packageAnalysisResult = packageAnalysisResults.GetValueOrDefault(new PackageVersionPair
            {
                PackageId = "Microsoft.CodeAnalysis.FxCopAnalyzers",
                Version = "2.9.8",
                PackageSourceType = PackageSourceType.NUGET
            }).Result;
            Assert.AreEqual(Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.
            GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.True(packageAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count > 0);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.AreEqual("2.9.9", packageAnalysisResult.Recommendations.RecommendedActions.First().Description);

            packageAnalysisResult = packageAnalysisResults.GetValueOrDefault(new PackageVersionPair
            {
                PackageId = "WebEssentials.AspNetCore.StaticFilesWithCache",
                Version = "1.0.1",
                PackageSourceType = PackageSourceType.NUGET
            }).Result;
            Assert.AreEqual(Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.
            GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.True(packageAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count > 0);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.AreEqual("1.0.3", packageAnalysisResult.Recommendations.RecommendedActions.First().Description);

            packageAnalysisResult = packageAnalysisResults.GetValueOrDefault(new PackageVersionPair
            {
                PackageId = "WebMarkupMin.AspNetCore2",
                Version = "2.7.0",
                PackageSourceType = PackageSourceType.NUGET
            }).Result;
            Assert.AreEqual(Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.
            GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.True(packageAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count > 0);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.AreEqual("2.8.0", packageAnalysisResult.Recommendations.RecommendedActions.First().Description);
        }

        [Test]
        public void CheckApiAnalysisResult()
        {
            solutionAnalysisResultTask.Wait();
            var sourceFileAnalysisResults = solutionAnalysisResultTask.Result.ProjectAnalysisResults.First().SourceFileAnalysisResults;
            var startupFile = sourceFileAnalysisResults.Find(s => s.SourceFileName == "Startup.cs");

            Assert.AreEqual(Path.Combine(_tmpTestProjectsExtractionPath, "Miniblog.Core-master",
                "src", "Startup.cs"), startupFile.SourceFilePath);
            var apiAnalysisResult = startupFile.ApiAnalysisResults.Find(r => r.CodeEntityDetails.OriginalDefinition
                == "Microsoft.Extensions.Hosting.IHostEnvironment.IsDevelopment()");
            Assert.AreEqual(CodeEntityType.Namespace, apiAnalysisResult.CodeEntityDetails.CodeEntityType);
            Assert.AreEqual("IsDevelopment", apiAnalysisResult.CodeEntityDetails.Name);
            Assert.AreEqual("Microsoft.Extensions.Hosting", apiAnalysisResult.CodeEntityDetails.Namespace);
            Assert.AreEqual("Microsoft.Extensions.Hosting.IHostEnvironment.IsDevelopment()",
                apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual("Microsoft.Extensions.Hosting.IHostEnvironment.IsDevelopment()", apiAnalysisResult.CodeEntityDetails.Signature);
            Assert.AreEqual("Microsoft.Extensions.Hosting.Abstractions", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("3.1.0", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual(PackageSourceType.SDK, apiAnalysisResult.CodeEntityDetails.Package.PackageSourceType);
            Assert.AreEqual(Compatibility.COMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.NoRecommendation, apiAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(apiAnalysisResult.Recommendations.RecommendedActions.First().Description);

            apiAnalysisResult = startupFile.ApiAnalysisResults.Find(r => r.CodeEntityDetails.OriginalDefinition
                == "Microsoft.AspNetCore.Builder.IApplicationBuilder.UseBrowserLink()");
            Assert.AreEqual(CodeEntityType.Namespace, apiAnalysisResult.CodeEntityDetails.CodeEntityType);
            Assert.AreEqual("UseBrowserLink", apiAnalysisResult.CodeEntityDetails.Name);
            Assert.AreEqual("Microsoft.AspNetCore.Builder", apiAnalysisResult.CodeEntityDetails.Namespace);
            Assert.AreEqual("Microsoft.AspNetCore.Builder.IApplicationBuilder.UseBrowserLink()",
                apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual("Microsoft.AspNetCore.Builder.IApplicationBuilder.UseBrowserLink()", apiAnalysisResult.CodeEntityDetails.Signature);
            Assert.AreEqual("Microsoft.VisualStudio.Web.BrowserLink", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("2.2.0", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual(PackageSourceType.NUGET, apiAnalysisResult.CodeEntityDetails.Package.PackageSourceType);
            Assert.AreEqual(Compatibility.COMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.NoRecommendation, apiAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(apiAnalysisResult.Recommendations.RecommendedActions.First().Description);

            apiAnalysisResult = startupFile.ApiAnalysisResults.Find(r => r.CodeEntityDetails.Package.PackageId == "WebOptimizer.Core" && 
                r.CodeEntityDetails.Signature == "Microsoft.AspNetCore.Builder.IApplicationBuilder.UseWebOptimizer()");
            Assert.AreEqual(CodeEntityType.Namespace, apiAnalysisResult.CodeEntityDetails.CodeEntityType);
            Assert.AreEqual("UseWebOptimizer", apiAnalysisResult.CodeEntityDetails.Name);
            Assert.AreEqual("Microsoft.AspNetCore.Builder", apiAnalysisResult.CodeEntityDetails.Namespace);
            Assert.AreEqual("Microsoft.AspNetCore.Builder.IApplicationBuilder.UseWebOptimizer()",
                apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual("Microsoft.AspNetCore.Builder.IApplicationBuilder.UseWebOptimizer()", apiAnalysisResult.CodeEntityDetails.Signature);
            Assert.AreEqual("WebOptimizer.Core", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("3.0.250", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual(PackageSourceType.NUGET, apiAnalysisResult.CodeEntityDetails.Package.PackageSourceType);
            Assert.AreEqual(Compatibility.INCOMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.NoRecommendation, apiAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(apiAnalysisResult.Recommendations.RecommendedActions.First().Description);

            apiAnalysisResult = startupFile.ApiAnalysisResults.Find(r => r.CodeEntityDetails.Signature == "Microsoft.AspNetCore.Builder.IApplicationBuilder.UseWebMarkupMin()");
            Assert.AreEqual(CodeEntityType.Namespace, apiAnalysisResult.CodeEntityDetails.CodeEntityType);
            Assert.AreEqual("UseWebMarkupMin", apiAnalysisResult.CodeEntityDetails.Name);
            Assert.AreEqual("WebMarkupMin.AspNetCore2", apiAnalysisResult.CodeEntityDetails.Namespace);
            Assert.AreEqual("Microsoft.AspNetCore.Builder.IApplicationBuilder.UseWebMarkupMin()",
                apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual("Microsoft.AspNetCore.Builder.IApplicationBuilder.UseWebMarkupMin()", apiAnalysisResult.CodeEntityDetails.Signature);
            Assert.AreEqual("WebMarkupMin.AspNetCore2", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("2.7.0", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual(PackageSourceType.NUGET, apiAnalysisResult.CodeEntityDetails.Package.PackageSourceType);
            Assert.AreEqual(Compatibility.COMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.True(apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count > 0);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, apiAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.AreEqual("2.8.0", apiAnalysisResult.Recommendations.RecommendedActions.First().Description);


            var blogController = sourceFileAnalysisResults.Find(s => s.SourceFileName == "BlogController.cs");
            Assert.AreEqual(Path.Combine(_tmpTestProjectsExtractionPath, "Miniblog.Core-master",
                "src", "Controllers", "BlogController.cs"), blogController.SourceFilePath);

            apiAnalysisResult = blogController.ApiAnalysisResults.Find(r => r.CodeEntityDetails.Signature
                == "System.Threading.Tasks.Task<Miniblog.Core.Models.Post?>.ConfigureAwait(bool)");
            Assert.AreEqual(CodeEntityType.Namespace, apiAnalysisResult.CodeEntityDetails.CodeEntityType);
            Assert.AreEqual("ConfigureAwait", apiAnalysisResult.CodeEntityDetails.Name);
            Assert.AreEqual("System.Threading.Tasks", apiAnalysisResult.CodeEntityDetails.Namespace);
            Assert.AreEqual("System.Threading.Tasks.Task<TResult>.ConfigureAwait(bool)",
                apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual("System.Threading.Tasks.Task<Miniblog.Core.Models.Post?>.ConfigureAwait(bool)", apiAnalysisResult.CodeEntityDetails.Signature);
            Assert.AreEqual("System.Runtime", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("4.2.2", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual(PackageSourceType.SDK, apiAnalysisResult.CodeEntityDetails.Package.PackageSourceType);
            Assert.AreEqual(Compatibility.COMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.NoRecommendation, apiAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(apiAnalysisResult.Recommendations.RecommendedActions.First().Description);

            apiAnalysisResult = blogController.ApiAnalysisResults.Find(r => r.CodeEntityDetails.Signature
                == "System.Collections.Generic.ICollection<Miniblog.Core.Models.Comment>.Add(Miniblog.Core.Models.Comment)");
            Assert.AreEqual(CodeEntityType.Namespace, apiAnalysisResult.CodeEntityDetails.CodeEntityType);
            Assert.AreEqual("Add", apiAnalysisResult.CodeEntityDetails.Name);
            Assert.AreEqual("System.Collections.Generic", apiAnalysisResult.CodeEntityDetails.Namespace);
            Assert.AreEqual("System.Collections.Generic.ICollection<T>.Add(T)",
                apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual("System.Collections.Generic.ICollection<Miniblog.Core.Models.Comment>.Add(Miniblog.Core.Models.Comment)", apiAnalysisResult.CodeEntityDetails.Signature);
            Assert.AreEqual("System.Runtime", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("4.2.2", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual(PackageSourceType.SDK, apiAnalysisResult.CodeEntityDetails.Package.PackageSourceType);
            Assert.AreEqual(Compatibility.COMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.NoRecommendation, apiAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(apiAnalysisResult.Recommendations.RecommendedActions.First().Description);

            var accountController = sourceFileAnalysisResults.Find(s => s.SourceFileName == "AccountController.cs");
            Assert.AreEqual(Path.Combine(_tmpTestProjectsExtractionPath, "Miniblog.Core-master",
                "src", "Controllers", "AccountController.cs"), accountController.SourceFilePath);

            apiAnalysisResult = accountController.ApiAnalysisResults.Find(r => r.CodeEntityDetails.Signature
                == "Microsoft.AspNetCore.Mvc.Controller.View()");
            Assert.AreEqual(CodeEntityType.Namespace, apiAnalysisResult.CodeEntityDetails.CodeEntityType);
            Assert.AreEqual("View", apiAnalysisResult.CodeEntityDetails.Name);
            Assert.AreEqual("Microsoft.AspNetCore.Mvc", apiAnalysisResult.CodeEntityDetails.Namespace);
            Assert.AreEqual("Microsoft.AspNetCore.Mvc.Controller.View()", apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual("Microsoft.AspNetCore.Mvc.Controller.View()", apiAnalysisResult.CodeEntityDetails.Signature);
            Assert.AreEqual("Microsoft.AspNetCore.Mvc.ViewFeatures", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("3.1.0", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual(PackageSourceType.SDK, apiAnalysisResult.CodeEntityDetails.Package.PackageSourceType);
            Assert.AreEqual(Compatibility.COMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").Compatibility);
            Assert.AreEqual(0, apiAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1").CompatibleVersions.Count);
            Assert.AreEqual(RecommendedActionType.NoRecommendation, apiAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);
            Assert.Null(apiAnalysisResult.Recommendations.RecommendedActions.First().Description);
        }
    }
}
