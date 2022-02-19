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

namespace PortingAssistant.Client.CLI

{
    class Program
    {
        private static string ProjectName => "PortingAssistant.Client.CLI";
        private static string ExeName => $"{ProjectName}.exe";
        private static string DllName => $"{ProjectName}.dll";

        static void Main(string[] args)
        {
            PortingAssistantCLI cli = new PortingAssistantCLI();
            cli.HandleCommand(args);

            var logConfiguration = new LoggerConfiguration().Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Console();

            var executingFile = GetAppEntryPoint();
            if (string.IsNullOrEmpty(executingFile))
            {
                var executingFileNotFoundMessageLines = new []
                {
                    "ERROR: Could not find one of the expected entry point files in the base directory.",
                    "Base Directory:",
                    AppContext.BaseDirectory,
                    "Expected one of the following files:",
                    DllName,
                    ExeName,
                    "Exiting the application"
                };
                var errorMessage = string.Join(Environment.NewLine, executingFileNotFoundMessageLines);
                Console.WriteLine(errorMessage);
                Environment.Exit(-1);
            }

            var executionPath = Path.GetDirectoryName(executingFile);
            var telemetryConfiguration = JsonSerializer.Deserialize<TelemetryConfiguration>(File.ReadAllText(Path.Combine(executionPath, "PortingAssistantTelemetryConfig.json")));

            var configuration = new PortingAssistantConfiguration();
            var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logs = Path.Combine(roamingFolder, "Porting Assistant for .NET", "logs");
            var logFilePath = Path.Combine(logs, "portingAssistant-client-cli.log");
            var metricsFilePath = Path.Combine(logs, "portingAssistant-client-cli.metrics");

            var version = FileVersionInfo.GetVersionInfo(executingFile).ProductVersion;

            var outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] (Porting Assistant Client CLI) (" + version + ") (" + cli.Tag + ") {SourceContext}: {Message:lj}{NewLine}{Exception}";

            Serilog.Formatting.Display.MessageTemplateTextFormatter tf =
                new Serilog.Formatting.Display.MessageTemplateTextFormatter(outputTemplate, CultureInfo.InvariantCulture);

            logConfiguration.WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Infinite,
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
                    UploadLogs(cli.Profile, telemetryConfiguration, logFilePath, metricsFilePath, logs);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "error when using the tools :");
                    UploadLogs(cli.Profile, telemetryConfiguration, logFilePath, metricsFilePath, logs);
                    Environment.Exit(-1);
                }
            }
        }

        private static string GetAppEntryPoint()
        {
            var executingDirectory = AppContext.BaseDirectory;
            var assemblyFile = Path.Combine(executingDirectory, DllName);
            var executableFile = Path.Combine(executingDirectory, ExeName);

            var entryPointFile = Directory.EnumerateFiles(executingDirectory, $"{ProjectName}.*")
                .FirstOrDefault(file => file.EndsWith(assemblyFile) || file.EndsWith(executableFile));
            return entryPointFile;
        }

        private static void UploadLogs(string profile, TelemetryConfiguration telemetryConfiguration, string logFilePath, string metricsFilePath, string logsPath)
        {
            if (!string.IsNullOrEmpty(profile))
            {
                bool isSuccessed = false;
                telemetryConfiguration.LogFilePath = logFilePath;
                telemetryConfiguration.MetricsFilePath = metricsFilePath;
                telemetryConfiguration.LogsPath = logsPath;

                if (TelemetryClientFactory.TryGetClient(profile, telemetryConfiguration, out ITelemetryClient client))
                {
                    isSuccessed = Uploader.Upload(telemetryConfiguration, profile, client);
                }
                else
                {
                    Log.Logger.Error("Invalid Credentials.");
                }               

                if (!isSuccessed)
                {
                    Log.Logger.Error("Upload Metrics/Logs Failed!");
                }
                else
                {
                    Log.Logger.Information("Upload Metrics/Logs Success!");
                }
            }
        }

        private static async Task<SolutionAnalysisResult> AnalyzeSolutionGenerator(IPortingAssistantClient portingAssistantClient, string solutionPath, AnalyzerSettings solutionSettings)
        {
            List<ProjectAnalysisResult> projectAnalysisResults = new List<ProjectAnalysisResult>();
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
        }
    }
}



