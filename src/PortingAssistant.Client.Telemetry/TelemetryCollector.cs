using Newtonsoft.Json;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.Telemetry.Model;
using Serilog;
using Serilog.Formatting.Compact;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Serilog.Templates;
using ILogger = Serilog.ILogger;

namespace PortingAssistantExtensionTelemetry
{
    public static class TelemetryCollector
    {
        private static string _filePath;
        private static ILogger _logger;
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

        public static void Builder(ILogger logger, string filePath)
        {
            if (_logger == null && _filePath == null)
            {
                _logger = logger;
                _filePath = filePath;
            }
            Log.Logger = new LoggerConfiguration().WriteTo.File(
                new ExpressionTemplate("{@m}\n"),
                _filePath,
                rollingInterval: RollingInterval.Month,
                rollOnFileSizeLimit: true
                                ).CreateLogger();
                        }
  
        private static void ConfigureDefault()
        {
            var AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var metricsFilePath = Path.Combine(AppData, "logs", "metrics.metrics");
            _filePath = metricsFilePath;
            var outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
            var logConfiguration = new LoggerConfiguration().Enrich.FromLogContext()
            .MinimumLevel.Warning()
            .WriteTo.File(
                Path.Combine(AppData, "logs", "metrics.log"),
                outputTemplate: outputTemplate);
            _logger = logConfiguration.CreateLogger();
        }

        private static void WriteToFile(string content)
        {
            _readWriteLock.EnterWriteLock();
            try
            {
                if (_filePath == null)
                {
                    ConfigureDefault();
                }
                using (StreamWriter sw = File.AppendText(_filePath))
                {
                    sw.WriteLine(content);
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to write to the metrics file with error", ex);
            }
            finally
            {
                // Release lock
                _readWriteLock.ExitWriteLock();
            }
        }

        public static void Collect<T>(T t)
        {
            //WriteToFile(JsonConvert.SerializeObject(t));
            Log.Logger.Information("{t}", JsonConvert.SerializeObject(t));
        }

        public static SolutionMetrics createSolutionMetric(SolutionDetails solutionDetail, string targetFramework, string version, string source, double analysisTime, string tag, SHA256 sha256hash, DateTime date) {
            
            return new SolutionMetrics
            {
                metricsType = MetricsType.solution,
                version = version,
                portingAssistantSource = source,
                tag = tag,
                targetFramework = targetFramework,
                timeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                solutionName = GetHash(sha256hash, solutionDetail.SolutionName),
                solutionPath = GetHash(sha256hash, solutionDetail.SolutionFilePath),
                ApplicationGuid = solutionDetail.ApplicationGuid,
                SolutionGuid = solutionDetail.SolutionGuid,
                RepositoryUrl = solutionDetail.RepositoryUrl,
                analysisTime = analysisTime,
            };
        }

        public static ProjectMetrics createProjectMetric(ProjectDetails project, string targetFramework, string version, string source, double analysisTime, string tag, SHA256 sha256hash, DateTime date
            ) {
            return new ProjectMetrics
            {
                metricsType = MetricsType.project,
                portingAssistantSource = source,
                tag = tag,
                version = version,
                targetFramework = targetFramework,
                sourceFrameworks = project.TargetFrameworks,
                timeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                projectName = GetHash(sha256hash, project.ProjectName),
                projectGuid = project.ProjectGuid,
                projectType = project.ProjectType,
                numNugets = project.PackageReferences.Count,
                numReferences = project.ProjectReferences.Count,
                isBuildFailed = project.IsBuildFailed,
                language = GetProjectLanguage(project.ProjectFilePath)
            };
            
        }

        public static NugetMetrics createNugetMetric(string targetFramework, string version, string source, double analysisTime, string tag, DateTime date, string packageId, string packageVersion, Compatibility compatibility) {
            return new NugetMetrics
            {
                metricsType = MetricsType.nuget,
                portingAssistantSource = source,
                tag = tag,
                version = version,
                targetFramework = targetFramework,
                timeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                pacakgeName = packageId,
                packageVersion = packageVersion,
                compatibility = compatibility,
            };
        }

        public static APIMetrics createAPIMetric(ApiAnalysisResult apiAnalysisResult, string targetFramework, string version, string source, string tag, DateTime date)
        { 
            return new APIMetrics
            {
                metricsType = MetricsType.api,
                portingAssistantSource = source,
                tag = tag,
                version = version,
                targetFramework = targetFramework,
                timeStamp = date.ToString("MM/dd/yyyy HH:mm"),
                name = apiAnalysisResult.CodeEntityDetails.Name,
                nameSpace = apiAnalysisResult.CodeEntityDetails.Namespace,
                originalDefinition = apiAnalysisResult.CodeEntityDetails.OriginalDefinition,
                compatibility = apiAnalysisResult.CompatibilityResults[targetFramework].Compatibility,
                packageId = apiAnalysisResult.CodeEntityDetails.Package.PackageId,
                packageVersion = apiAnalysisResult.CodeEntityDetails.Package.Version
            };
        }

        public static void SolutionAssessmentCollect(SolutionAnalysisResult result, string targetFramework, string version, string source, double analysisTime, string tag)
        {
            var sha256hash = SHA256.Create();
            var date = DateTime.Now;
            var solutionDetail = result.SolutionDetails;
            // Solution Metrics
            var solutionMetrics = createSolutionMetric(solutionDetail, targetFramework, version, source, analysisTime, tag, sha256hash, date);
            TelemetryCollector.Collect<SolutionMetrics>(solutionMetrics);

            foreach (var project in solutionDetail.Projects)
            {
                var projectMetrics = createProjectMetric(project, targetFramework, version, source, analysisTime, tag, sha256hash, date);
                TelemetryCollector.Collect<ProjectMetrics>(projectMetrics);
            }

            //nuget metrics
            result.ProjectAnalysisResults.ForEach(project =>
            {
                foreach (var nuget in project.PackageAnalysisResults)
                {
                    nuget.Value.Wait();
                    var packageID = nuget.Value.Result.PackageVersionPair.PackageId;
                    var packageVersion = nuget.Value.Result.PackageVersionPair.Version;
                    var compatability = nuget.Value.Result.CompatibilityResults[targetFramework].Compatibility;
                    var nugetMetrics = createNugetMetric(targetFramework, version, source, analysisTime, tag, date, packageID, packageVersion, compatability);
                    TelemetryCollector.Collect<NugetMetrics>(nugetMetrics);
                }

                foreach (var sourceFile in project.SourceFileAnalysisResults)
                {
                    FileAssessmentCollect(sourceFile, targetFramework, version, source, tag);
                }
            });
        }


        public static void FileAssessmentCollect(SourceFileAnalysisResult result, string targetFramework, string version, string source, string tag)
        {
            var date = DateTime.Now;
            foreach (var api in result.ApiAnalysisResults)
            {
                var apiMetrics = createAPIMetric(api, targetFramework, version, source, tag, date);
                TelemetryCollector.Collect<APIMetrics>(apiMetrics);
            }
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, string input)

        {

            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            var sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)

            {

                sBuilder.Append(data[i].ToString("x2"));

            }

            return sBuilder.ToString();

        }

        private static string GetProjectLanguage(string projectFilePath)
        {
            if (projectFilePath.EndsWith(".csproj", StringComparison.InvariantCultureIgnoreCase))
            {
                return "csharp";
            }
            if (projectFilePath.EndsWith(".vbproj", StringComparison.InvariantCultureIgnoreCase))
            {
                return "visualbasic";
            }
            return "invalid";
        }
    }
}
