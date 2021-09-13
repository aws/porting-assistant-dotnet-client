﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.Common;
using PortingAssistant.Client.Telemetry.Model;
using PortingAssistantExtensionTelemetry;
using Serilog;

namespace PortingAssistant.Client.CLI

{
    class Program
    {
        static void Main(string[] args)
        {

            PortingAssistantCLI cli = new PortingAssistantCLI();
            cli.HandleCommand(args);

            var logConfiguration = new LoggerConfiguration().Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Console();

            if (cli.isSchema)
            {
                if (cli.schemaVersion)
                {
                    Console.Out.WriteLine(Common.Model.Schema.version);
                }
            }

            if (cli.isAssess)
            {
                try
                {
                    var configuration = new PortingAssistantConfiguration();
                    var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var driftLogs = Path.Combine(roamingFolder, "Porting Assistant for .NET", "logs", "drift");
                    var logFilePath = Path.Combine(driftLogs, "portingAssistant-assessment.log");
                    var metricsFilePath = Path.Combine(driftLogs, "portingAssistant-assessment.metrics");

                    var outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

                    Serilog.Formatting.Display.MessageTemplateTextFormatter tf =
                        new Serilog.Formatting.Display.MessageTemplateTextFormatter(outputTemplate, CultureInfo.InvariantCulture);

                    logConfiguration.WriteTo.File(
                            logFilePath,
                            rollingInterval: RollingInterval.Infinite,
                            rollOnFileSizeLimit: false,
                            outputTemplate: outputTemplate);

                    Log.Logger = logConfiguration.CreateLogger();
                    TelemetryCollector.Builder(Log.Logger, metricsFilePath);

                    var portingAssistantBuilder = PortingAssistantBuilder.Build(configuration, logConfig =>
                        logConfig.SetMinimumLevel(LogLevel.Debug)
                        .AddSerilog(logger: Log.Logger, dispose: true));

                    var portingAssistantClient = portingAssistantBuilder.GetPortingAssistant();
                    var reportExporter = portingAssistantBuilder.GetReportExporter();
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

                    var startTime = DateTime.Now;
                    var analyzeResults = portingAssistantClient.AnalyzeSolutionAsync(cli.SolutionPath, solutionSettings);
                    analyzeResults.Wait();
                    if (analyzeResults.IsCompletedSuccessfully)
                    {
                        reportExporter.GenerateJsonReport(analyzeResults.Result, cli.OutputPath);
                        TelemetryCollector.SolutionAssessmentCollect(analyzeResults.Result, cli.Target, "1.0.0", "PortingAssistantCLI", DateTime.Now.Subtract(startTime).TotalMilliseconds);
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
                            .Where(package =>
                            {
                                var comp = package.Result.CompatibilityResults.GetValueOrDefault(cli.Target);
                                return comp.Compatibility != Compatibility.COMPATIBLE && comp.CompatibleVersions.Count != 0;
                            })
                            .SelectMany(package => package.Result.Recommendations.RecommendedActions));
                        var PortingRequest = new PortingRequest
                        {

                            Projects = analyzeResults.Result.SolutionDetails.Projects.Where(p => cli.PortingProjects.Contains(p.ProjectFilePath)).ToList(),
                            SolutionPath = cli.SolutionPath,
                            TargetFramework = cli.Target.ToString(),
                            RecommendedActions = FilteredRecommendedActions.ToList(),
                            IncludeCodeFix = true
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
}



