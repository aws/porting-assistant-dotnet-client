using Microsoft.Extensions.DependencyInjection;
using Serilog;
using PortingAssistant.Client.Handler;
using System.Threading.Tasks;
using PortingAssistant.Client.Model;
using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using Serilog.Sinks.SystemConsole.Themes;
using PortingAssistant.Client.NuGet;

namespace IntegrationTests
{
    public class NetFrameworkTests
    {
        private Task<SolutionAnalysisResult> solutionAnalysisResult;
        [SetUp]
        public void Setup()
        {
            var config = @"C:\workspace\PortingAssistant\src\Dotnetmod\packages\electron\build-scripts\encore-config.dev.json";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            var serviceConfig = new ConfigurationBuilder()
                .AddJsonFile(config)
                .Build();
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, serviceConfig);

            var services = serviceCollection.BuildServiceProvider();

            var portingAssistantHandler = services.GetService<IPortingAssistantHandler>();

            var solutionPath = @"C:\workspace\PortingAssistant\src\PortingAssistantClient\PortingAssistantClient\NetFramworkExample\NetFramworkExample.sln";
            
            solutionAnalysisResult = portingAssistantHandler.AnalyzeSolutionAsync(solutionPath, new Settings());
        }

        static private void ConfigureServices(IServiceCollection serviceCollection, IConfiguration config)
        {
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            serviceCollection.AddAssessment(config.GetSection("AnalyzerConfiguration"));
            serviceCollection.AddOptions();
        }

        [Test]
        public void Test1()
        {
            //solutionAnalysisResult.Wait();
            Assert.True(true);
        }
    }
}