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
using PortingAssistant.Client.Common.Utils;
using System.Runtime.CompilerServices;
using System.Threading;
using Serilog.Core;


namespace PortingAssistant.Client.CLI

{
    public class Program
    {
        public static void Main(string[] args)
        {
            PortingAssistantCLI cli = new PortingAssistantCLI();
            cli.HandleCommand(args);

            var levelSwitch = new LoggingLevelSwitch();
            levelSwitch.MinimumLevel = cli.MinimumLoggingLevel;

            var logConfiguration = new LoggerConfiguration().Enrich.FromLogContext()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Console();

            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var telemetryConfiguration = JsonSerializer.Deserialize<TelemetryConfiguration>(File.ReadAllText(Path.Combine(assemblyPath, "PortingAssistantTelemetryConfig.json")));
            if (!string.IsNullOrEmpty(cli.EgressPoint))
            {
                telemetryConfiguration.InvokeUrl = cli.EgressPoint;
                Console.WriteLine($"Change endpoint to {telemetryConfiguration.InvokeUrl}");
            }

            var configuration = new PortingAssistantConfiguration();
            var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logs = Path.Combine(roamingFolder, "Porting Assistant for .NET", "logs");
            var logFilePath = Path.Combine(logs, "portingAssistant-client-cli-.log");
            var metricsFilePath = Path.Combine(logs, "portingAssistant-client-cli-.metrics");

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
                            TargetFramework = cli.Target,
                            UseGenerator = cli.UseGenerator
                        }
                        : new AnalyzerSettings
                        {
                            IgnoreProjects = new List<string>(),
                            TargetFramework = cli.Target,
                            UseGenerator = cli.UseGenerator
                        };

                    var startTime = DateTime.Now;
                    Task<SolutionAnalysisResult> analyzeResults;

                    // Assess solution
                    if (solutionSettings.UseGenerator)
                    {
                        var cancellationTokenSource = new CancellationTokenSource();
                        analyzeResults = AnalyzeSolutionGenerator(portingAssistantClient, cli.SolutionPath, solutionSettings, cancellationTokenSource.Token);
                    }
                    else
                    {
                        analyzeResults = portingAssistantClient.AnalyzeSolutionAsync(cli.SolutionPath, solutionSettings);
                        analyzeResults.Wait();
                    }

                    // Collect telemetry
                    if (analyzeResults.IsCompletedSuccessfully)
                    {
                        TraceEvent.Start(Log.Logger, $"Telemetry collection for {cli.SolutionPath}");
                        reportExporter.GenerateJsonReport(analyzeResults.Result, cli.OutputPath);
                        TelemetryCollector.SolutionAssessmentCollect(analyzeResults.Result, cli.Target, "1.8.0", "Porting Assistant Client CLI", DateTime.Now.Subtract(startTime).TotalMilliseconds, cli.Tag);
                        TraceEvent.End(Log.Logger, $"Telemetry collection for {cli.SolutionPath}");
                    }
                    else
                    {
                        Log.Logger.Error("err generated solution analysis report");
                    }

                    // Port projects
                    if (cli.PortingProjects != null && cli.PortingProjects.Count != 0)
                    {
                        var portingProjectResults = analyzeResults.Result.ProjectAnalysisResults
                            .Where(project => cli.PortingProjects.Contains(project.ProjectName));
                        var filteredRecommendedActions = portingProjectResults
                            .SelectMany(project => project.PackageAnalysisResults.Values
                            .Where(package =>
                            {
                                var comp = package.Result.CompatibilityResults.GetValueOrDefault(cli.Target);
                                return comp.Compatibility != Compatibility.COMPATIBLE && comp.CompatibleVersions.Count != 0;
                            })
                            .SelectMany(package => package.Result.Recommendations.RecommendedActions));
                        var portingRequest = new PortingRequest
                        {
                            Projects = analyzeResults.Result.SolutionDetails.Projects.Where(p => cli.PortingProjects.Contains(p.ProjectName)).ToList(),
                            SolutionPath = cli.SolutionPath,
                            TargetFramework = cli.Target,
                            RecommendedActions = filteredRecommendedActions.ToList(),
                            IncludeCodeFix = true,
                            VisualStudioVersion = solutionSettings.VisualStudioVersion
                        };

                        TraceEvent.Start(Log.Logger, $"Applying porting actions to projects in {cli.SolutionPath}");
                        var portingResults = portingAssistantClient.ApplyPortingChanges(portingRequest);
                        reportExporter.GenerateJsonReport(portingResults, cli.SolutionPath, cli.OutputPath);
                        TraceEvent.End(Log.Logger, $"Applying porting actions to projects in {cli.SolutionPath}");
                    }

                    TraceEvent.Start(Log.Logger, $"Upload telemetry for {cli.SolutionPath}");
                    UploadLogs(cli.Profile, telemetryConfiguration, logFilePath, metricsFilePath, logs, cli.EnabledDefaultCredentials);
                    TraceEvent.End(Log.Logger, $"Upload telemetry for {cli.SolutionPath}");
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "error when using the tools :");
                    UploadLogs(cli.Profile, telemetryConfiguration, logFilePath, metricsFilePath, logs, cli.EnabledDefaultCredentials);
                    Environment.Exit(-1);
                }
            }
        }

        private static void UploadLogs(
            string profile, 
            TelemetryConfiguration telemetryConfiguration, 
            string logFilePath, 
            string metricsFilePath, 
            string logsPath, 
            bool enabledDefaultCredentials = false)
        {
            if (string.IsNullOrEmpty(profile))
            {
                return;
            }
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

        public static async Task<SolutionAnalysisResult> AnalyzeSolutionGenerator(
            IPortingAssistantClient portingAssistantClient, 
            string solutionPath, 
            AnalyzerSettings solutionSettings,
            CancellationToken cancellationToken = default)
        {
            try 
            {
                var projectAnalysisResults = new List<ProjectAnalysisResult>();
                var failedProjects = new List<string>();
                var projectAnalysisResultEnumerator = portingAssistantClient.AnalyzeSolutionGeneratorAsync(solutionPath, solutionSettings).GetAsyncEnumerator(cancellationToken);

                while (await projectAnalysisResultEnumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
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
                        IsBuildFailed = p.IsBuildFailed,
                        LinesOfCode = p.LinesOfCode
                    }),
                    FailedProjects = failedProjects
                };

                return new SolutionAnalysisResult
                {
                    FailedProjects = failedProjects,
                    SolutionDetails = solutionDetails,
                    ProjectAnalysisResults = projectAnalysisResults
                };
            } 
            catch (TaskCanceledException ex)
            {
                throw new PortingAssistantException($"Analyze solution Cancelled {solutionPath}", ex);
            }
            catch (Exception ex) 
            {
                throw new PortingAssistantException($"Cannot Analyze solution {solutionPath}", ex);
            }
        }
    }
}
