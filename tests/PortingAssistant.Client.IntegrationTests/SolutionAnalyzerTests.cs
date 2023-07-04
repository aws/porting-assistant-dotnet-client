using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Threading;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.CLI;
using NUnit.Framework.Internal.Execution;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using NUnit.Framework.Internal;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace PortingAssistant.Client.IntegrationTests
{

    [TestFixture]
    public class SolutionAnalyzerTests
    {
        private IPortingAssistantClient portingAssistantClient;
        private string _tmpTestProjectsExtractionPath;
        private string _vbTmpTestProjectsExtractionPath;
        private IAsyncEnumerator<ProjectAnalysisResult> solutionAnalysisGenerator;
        private string netFrameworkProjectPath;
        [OneTimeSetUp]
        public void OneTimeSetUp()
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


            netFrameworkProjectPath = Path.Combine(_tmpTestProjectsExtractionPath, "NetFrameworkExample", "NetFrameworkExample.sln");
        }

        [Test]
        [TestCase("netcoreapp3.1")]
        [TestCase("net5.0")]
        [TestCase("net6.0")]
        [TestCase("net7.0")]
        public void AnalyzeSolutionGenerator_ShouldThrowPortingAssistantException_WhenCancellationRequested( string targetFramework)
        {
            // Arrange
            var solutionSettings = new AnalyzerSettings()
            {
                TargetFramework = targetFramework,
                UseGenerator = true
            };
            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(100);

            // Act and Assert
            var exception = Assert.ThrowsAsync<PortingAssistantException>(async () =>
            await Program.AnalyzeSolutionGenerator(
                    portingAssistantClient,
                    netFrameworkProjectPath,
                    solutionSettings,
                    cancellationTokenSource.Token
                )
            );
            Assert.That(exception.InnerException.Message, Is.EqualTo("The operation was canceled."));
        }

        [Test]
        [TestCase("netcoreapp3.1")]
        [TestCase("net5.0")]
        [TestCase("net6.0")]
        [TestCase("net7.0")]
        public void AnalyzeSolutionGenerator_WithoutCancellationSucceeds(string targetFramework)
        {
            // Arrange
            var solutionSettings = new AnalyzerSettings()
            {
                TargetFramework = targetFramework,
                UseGenerator = true
            };

            // Act and Assert
            Assert.DoesNotThrowAsync(() =>
            Program.AnalyzeSolutionGenerator(
                    portingAssistantClient,
                    netFrameworkProjectPath,
                    solutionSettings
                    )
            );
        }

        static private void ConfigureServices(IServiceCollection serviceCollection, PortingAssistantConfiguration config)
        {
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
            serviceCollection.AddAssessment(config);
            serviceCollection.AddOptions();
        }


    }
}
