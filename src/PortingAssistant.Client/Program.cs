using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using PortingAssistant.Client.Handler;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.Reports;

namespace PortingAssistant.Client.Client
{

    class Program

    {
        static async Task Main(string[] args)
        {
            PortingAssistantCLI cli = new PortingAssistantCLI();
            cli.HandleCommand(args);
            var config = @"config.json";
            var serviceConfig = new ConfigurationBuilder()
                .AddJsonFile(config)
                .Build();
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, serviceConfig);

            try
            {
                var services = serviceCollection.BuildServiceProvider();
                var logger = services.GetRequiredService<ILogger<Program>>();
                var portingAssistantHandler = services.GetService<IPortingAssistantHandler>();
                var reportHandler = services.GetService<IReportHandler>();
                var settings = cli.IgnoreProjects != null && cli.IgnoreProjects.Count != 0 ?
                        new Settings
                        {
                            IgnoreProjects = cli.IgnoreProjects
                        } : new Settings
                        {
                            IgnoreProjects = new List<string>()
                        };
                var analyzeResults = portingAssistantHandler.AnalyzeSolutionAsync(cli.SolutionPath, settings);
                analyzeResults.Wait();
                if (analyzeResults.IsCompletedSuccessfully)
                {
                    reportHandler.GenerateJsonReport(analyzeResults.Result, cli.OutputPath);
                }
                else
                {
                    logger.LogError("err generated solution analysis report");
                }
                if (cli.PortingProjects != null)
                {
                    var PortingProjectResults = analyzeResults.Result.ProjectAnalysisResults
                        .Where(project => cli.PortingProjects.Contains(project.ProjectName));
                    var FilteredRecommendedActions = PortingProjectResults
                        .SelectMany(project => project.PackageAnalysisResults.Values
                        .SelectMany(package => package.Result.Recommendations.RecommendedActions));
                    var PortingRequest = new PortingRequest
                    {
                        ProjectPaths = cli.PortingProjects,
                        SolutionPath = cli.SolutionPath,
                        TargetFramework = cli.Target.ToString(),
                        RecommendedActions = FilteredRecommendedActions.ToList()
                    };
                    var portingResults = portingAssistantHandler.ApplyPortingChanges(PortingRequest);
                    await reportHandler.GenerateJsonReport(portingResults, cli.SolutionPath, cli.OutputPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error when using the tools :" + ex);
            }
        }

        static private void ConfigureServices(IServiceCollection serviceCollection, IConfiguration config)
        {
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
            serviceCollection.AddAssessment(config.GetSection("AnalyzerConfiguration"));

            serviceCollection.AddSingleton<IReportHandler, ReportHandler>();

            serviceCollection.AddOptions();

        }
    }

}

