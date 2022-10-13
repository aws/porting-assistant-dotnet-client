using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistantExtensionTelemetry;
using PortingAssistantExtensionTelemetry.Model;
using Serilog;
using System.Threading.Tasks;
using System.Text.Json;
using PortingAssistant.Client.Telemetry;
using System.Diagnostics;
using System.Reflection;

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

            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var telemetryConfiguration = JsonSerializer.Deserialize<TelemetryConfiguration>(File.ReadAllText(Path.Combine(assemblyPath, "PortingAssistantTelemetryConfig.json")));

            var configuration = new PortingAssistantConfiguration();
            var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logs = Path.Combine(roamingFolder, "Porting Assistant for .NET", "logs");
            var logFilePath = Path.Combine(logs, "portingAssistant-client-cli-.log");
            var metricsFilePath = Path.Combine(logs, $"portingAssistant-client-cli-{DateTime.Today:yyyyMMdd}.metrics");

            var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            var outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] (Porting Assistant Client CLI) (" + version + ") (" + cli.Tag + ") {SourceContext}: {Message:lj}{NewLine}{Exception}";

            Serilog.Formatting.Display.MessageTemplateTextFormatter tf =
                new Serilog.Formatting.Display.MessageTemplateTextFormatter(outputTemplate, CultureInfo.InvariantCulture);

            logConfiguration.WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: false,
                    outputTemplate: outputTemplate);
            Log.Logger = logConfiguration.CreateLogger();

            if (cli.isSchema)
            {
                if (cli.schemaVersion)
                {
                    Console.WriteLine(Common.Model.Schema.version);
                }
            }

            if (cli.isAssess)
            {
                try
                {
                    TelemetryCollector.Builder(Log.Logger, metricsFilePath);

                    var portingAssistantBuilder = PortingAssistantBuilder.Build(configuration, logConfig =>
                        logConfig.SetMinimumLevel(LogLevel.Debug)
                        .AddSerilog(logger: Log.Logger, dispose: true));

                    var portingAssistantClient = portingAssistantBuilder.GetPortingAssistant();
                    var reportExporter = portingAssistantBuilder.GetReportExporter();
                    var solutionSettings = cli.IgnoreProjects != null && cli.IgnoreProjects.Count != 0
                        ? new AnalyzerSettings
                        {
                            IgnoreProjects = cli.IgnoreProjects,
                            TargetFramework = cli.Target
                        }
                        : new AnalyzerSettings
                        {
                            IgnoreProjects = new List<string>(),
                            TargetFramework = cli.Target
                        };

                    var startTime = DateTime.Now;
                    Task<SolutionAnalysisResult> analyzeResults;

                    if (solutionSettings.UseGenerator)
                    {
                        analyzeResults = AnalyzeSolutionGenerator(portingAssistantClient, cli.SolutionPath, solutionSettings);
                    }
                    else
                    {
                        analyzeResults = portingAssistantClient.AnalyzeSolutionAsync(cli.SolutionPath, solutionSettings);
                        analyzeResults.Wait();
                    }
                    if (analyzeResults.IsCompletedSuccessfully)
                    {
                        reportExporter.GenerateJsonReport(analyzeResults.Result, cli.OutputPath);
                        TelemetryCollector.SolutionAssessmentCollect(analyzeResults.Result, cli.Target, "1.8.0", $"Porting Assistant Client CLI", DateTime.Now.Subtract(startTime).TotalMilliseconds, cli.Tag);
                    }
                    else
                    {
                        Log.Logger.Error("err generated solution analysis report");
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

                            Projects = analyzeResults.Result.SolutionDetails.Projects.Where(p => cli.PortingProjects.Contains(p.ProjectName)).ToList(),
                            SolutionPath = cli.SolutionPath,
                            TargetFramework = cli.Target.ToString(),
                            RecommendedActions = FilteredRecommendedActions.ToList(),
                            IncludeCodeFix = true
                        };
                        var portingResults = portingAssistantClient.ApplyPortingChanges(PortingRequest);
                        reportExporter.GenerateJsonReport(portingResults, cli.SolutionPath, cli.OutputPath);

                    }
                    UploadLogs(cli.Profile, telemetryConfiguration, logFilePath, metricsFilePath, logs, cli.EnabledDefaultCredentials);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "error when using the tools :");
                    UploadLogs(cli.Profile, telemetryConfiguration, logFilePath, metricsFilePath, logs, cli.EnabledDefaultCredentials);
                    Environment.Exit(-1);
                }
            }
        }

        private static void UploadLogs(string profile, TelemetryConfiguration telemetryConfiguration, string logFilePath, string metricsFilePath, string logsPath, bool enabledDefaultCredentials = false)
        {
            telemetryConfiguration.LogFilePath = logFilePath;
            telemetryConfiguration.MetricsFilePath = metricsFilePath;
            telemetryConfiguration.LogsPath = logsPath;
            telemetryConfiguration.Suffix = new List<string> {".log", ".metrics"};
            telemetryConfiguration.LogPrefix = "portingAssistant-client-cli";
            bool shareMetrics = !string.IsNullOrEmpty(profile) || enabledDefaultCredentials;
            bool uploadSuccess = false;
            if (TelemetryClientFactory.TryGetClient(profile, telemetryConfiguration, out ITelemetryClient client, enabledDefaultCredentials))
            {
                var uploader = new Uploader(telemetryConfiguration, client, Log.Logger, shareMetrics);
                uploadSuccess = uploader.Run();
                uploader.WriteLogUploadErrors();
            }
            if (uploadSuccess)
            {
                Log.Logger.Information("Upload Metrics/Logs Success!");
            }
            else
            {
                Log.Logger.Error("Upload Metrics/Logs Failed!");
            }
        }

        private static async Task<SolutionAnalysisResult> AnalyzeSolutionGenerator(IPortingAssistantClient portingAssistantClient, string solutionPath, AnalyzerSettings solutionSettings)
        {
            try {
                var projectAnalysisResults = new List<ProjectAnalysisResult>();
                var failedProjects = new List<string>();
                var projectAnalysisResultEnumerator = portingAssistantClient.AnalyzeSolutionGeneratorAsync(solutionPath, solutionSettings).GetAsyncEnumerator();

                while (await projectAnalysisResultEnumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var result = projectAnalysisResultEnumerator.Current;
                    projectAnalysisResults.Add(result);

                    if (result.IsBuildFailed)
                    {
                        failedProjects.Add(result.ProjectFilePath);
                    }
                }


                var solutionDetails = new SolutionDetails
                {
                    SolutionName = Path.GetFileNameWithoutExtension(solutionPath),
                    SolutionFilePath = solutionPath,
                    Projects = projectAnalysisResults.ConvertAll(p => new ProjectDetails
                    {
                        PackageReferences = p.PackageReferences,
                        ProjectFilePath = p.ProjectFilePath,
                        ProjectGuid = p.ProjectGuid,
                        ProjectName = p.ProjectName,
                        ProjectReferences = p.ProjectReferences,
                        ProjectType = p.ProjectType,
                        TargetFrameworks = p.TargetFrameworks,
                        IsBuildFailed = p.IsBuildFailed
                    }),

                    FailedProjects = failedProjects
                };

                return new SolutionAnalysisResult
                {
                    FailedProjects = failedProjects,
                    SolutionDetails = solutionDetails,
                    ProjectAnalysisResults = projectAnalysisResults
                };
            } catch (Exception ex) {
                throw new PortingAssistantException($"Cannot Analyze solution {solutionPath}", ex);
            }
        }
    }
}
