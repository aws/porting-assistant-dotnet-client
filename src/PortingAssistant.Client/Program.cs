using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.CLI

{
    class Program
    {
        static void Main(string[] args)
        {

            PortingAssistantCLI cli = new PortingAssistantCLI();
            cli.HandleCommand(args);
            try
            {
                var configuration = new AnalyzerConfiguration()
                {
                    UseDataStoreSettings = true,
                    UseInternalNuGetServer = false,
                    DataStoreSettings = new DataStoreSettings
                    {
                        HttpsEndpoint = "https://s3.us-west-2.amazonaws.com/aws.portingassistant.dotnet.datastore/",
                        S3Endpoint = "aws.portingassistant.dotnet.datastore"
                    },
                    InternalNuGetServerSettings = new NuGetServerSettings
                    {
                        NugetServerEndpoint = "NugetServerEndpoint",
                    }
                };
                var portingAssistantBuilder = PortingAssistantBuilder.Build(configuration, logConfig => logConfig.AddConsole());
                var portingAssistantClient = portingAssistantBuilder.GetPortingAssistant();
                var reportExporter = portingAssistantBuilder.GetReportExporter();
                var solutiongSettings = cli.IgnoreProjects != null && cli.IgnoreProjects.Count != 0 ?
                        new PortingAssistantSettings
                        {
                            IgnoreProjects = cli.IgnoreProjects
                        } : new PortingAssistantSettings
                        {
                            IgnoreProjects = new List<string>()
                        };

                var analyzeResults = portingAssistantClient.AnalyzeSolutionAsync(cli.SolutionPath, solutiongSettings);
                analyzeResults.Wait();
                if (analyzeResults.IsCompletedSuccessfully)
                {
                    reportExporter.GenerateJsonReport(analyzeResults.Result, cli.OutputPath);
                }
                else
                {
                    Console.WriteLine("err generated solution analysis report");
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
                    var portingResults = portingAssistantClient.ApplyPortingChanges(PortingRequest);
                    reportExporter.GenerateJsonReport(portingResults, cli.SolutionPath, cli.OutputPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error when using the tools :" + ex);
            }
        }
    }
}



