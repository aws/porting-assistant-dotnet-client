using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using Google.Protobuf;
using com.amazon.awsassessment.analysis.v2;
using static com.amazon.awsassessment.analysis.v2.SourceCodeAnalyzerOutputV2.Types;
using static com.amazon.awsassessment.analysis.v2.AntipatternInstance.Types;
using static com.amazon.awsassessment.analysis.v2.SourceCodeAnalyzerOutputV2.Types.SourceCodeAnalyzerOutputEntry.Types;
using com.amazon.awsassessment.analysis.io;

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
                var configuration = new PortingAssistantConfiguration();
                var portingAssistantBuilder = PortingAssistantBuilder.Build(configuration, logConfig => logConfig.AddConsole());
                var portingAssistantClient = portingAssistantBuilder.GetPortingAssistant();
                var reportExporter = portingAssistantBuilder.GetReportExporter();
                var protoInput = new SourceCodeAnalyzerInput();
                var isProto = cli.ProtoFilePath != null;
                if (isProto)
                {
                    using (var input = File.OpenRead(cli.ProtoFilePath))
                    {
                        protoInput = SourceCodeAnalyzerInput.Parser.ParseFrom(input);
                    }
                }

                var solutionSettings = cli.IgnoreProjects != null && cli.IgnoreProjects.Count != 0 ?
                        new AnalyzerSettings
                        {
                            IgnoreProjects = cli.IgnoreProjects,
                            TargetFramework = cli.Target
                        } : new AnalyzerSettings
                        {
                            IgnoreProjects = new List<string>(),
                            TargetFramework = cli.Target
                        };

                var solutionPath = isProto ? protoInput.InputV2.AbsoluteProjectFilePath : cli.SolutionPath;
                var outputPath = isProto ? protoInput.InputV2.AbsoluteOutputFilePath : cli.OutputPath;
                var analyzeResults = portingAssistantClient.AnalyzeSolutionAsync(solutionPath, solutionSettings);
                analyzeResults.Wait();
                if (analyzeResults.IsCompletedSuccessfully)
                {
                    if (isProto)
                    {
                        reportExporter.GenerateProtoReport(analyzeResults.Result, Path.Combine(outputPath, "result.data"));
                    }
                    else
                    {
                        reportExporter.GenerateJsonReport(analyzeResults.Result, outputPath);
                    }
                }
                else
                {
                    Console.WriteLine("err generated solution analysis report");
                }
                if (cli.PortingProjects != null && cli.PortingProjects.Count != 0)
                {

                    var PortingProjectResults = analyzeResults.Result.ProjectAnalysisResults
                        .Where(project => cli.PortingProjects.Contains(project.ProjectName));
                    var FilteredRecommendedActions = PortingProjectResults
                        .SelectMany(project => project.PackageAnalysisResults.Values
                        .SelectMany(package => package.Result.Recommendations.RecommendedActions));
                    var PortingRequest = new PortingRequest
                    {

                        Projects = analyzeResults.Result.SolutionDetails.Projects.Where(p => cli.PortingProjects.Contains(p.ProjectFilePath)).ToList(),
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



