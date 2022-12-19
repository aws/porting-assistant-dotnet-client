using NUnit.Framework;
using PortingAssistant.Client.Client.Utils;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortingAssistant.Client.Client.Reports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace PortingAssistant.Client.UnitTests
{
    public class ReportExporterTest
    {
        private string testDirectoryRoot;
        private string tmpTestFixturePath;
        private ILogger<ReportExporter> testLogger;
        private IReportExporter _reportExporter;
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            testDirectoryRoot = TestContext.CurrentContext.TestDirectory;
            tmpTestFixturePath = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName()));
            Directory.CreateDirectory(tmpTestFixturePath);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IReportExporter, ReportExporter>();
            var serviceProvider = serviceCollection
                                    .AddLogging()
                                    .BuildServiceProvider();
            var factory = serviceProvider.GetService<ILoggerFactory>();
            testLogger = factory.CreateLogger<ReportExporter>();
            testLogger.LogInformation("Total logs");
            _reportExporter = new ReportExporter(testLogger);

        }
        [Test]
        public void GenerateJsonReportTest()
        {
            var solutionName = "test";
            var portingResults = new List<PortingResult> { new PortingResult { ProjectFile = "test", Message = "test", ProjectName = "test", Success = true } };
            Assert.DoesNotThrow(() =>
            {
                _reportExporter.GenerateJsonReport(portingResults, solutionName, tmpTestFixturePath);
            }
            );
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Directory.Delete(tmpTestFixturePath, true);
        }
    }
}
