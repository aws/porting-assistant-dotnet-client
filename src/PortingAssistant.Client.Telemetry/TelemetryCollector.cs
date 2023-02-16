using Newtonsoft.Json;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.Telemetry.Model;
using Serilog;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Serilog.Templates;
using ILogger = Serilog.ILogger;
using System.Linq;
using System.Net.NetworkInformation;

namespace PortingAssistantExtensionTelemetry
{
    public static class TelemetryCollector
    {
        private static string _filePath;
        private static ILogger _logger;
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        private static ILogger metricsLogger;
        private static int _numLogicalCores;
        private static double _systemMemory;
        private static string _sessionId;
        private static SHA256 _sha256hash;

        public static void Builder(ILogger logger, string filePath)
        {
            if (_logger == null && _filePath == null && _sessionId == null)
            {
                _logger = logger;
                _filePath = filePath;
                _sessionId = Guid.NewGuid().ToString();
            }
            metricsLogger = new LoggerConfiguration().WriteTo.File(
                new ExpressionTemplate("{@m}\n"),
                _filePath,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true
                                ).CreateLogger();
            _numLogicalCores = Environment.ProcessorCount;
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            var installedMemory = gcMemoryInfo.TotalAvailableMemoryBytes;
            _systemMemory = (double)installedMemory / 1048576.0;
            _sha256hash = SHA256.Create();
        }

        public static void Collect<T>(T t)
        {
            metricsLogger.Information("{t}", JsonConvert.SerializeObject(t));
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
                linesOfCode = solutionDetail.Projects.Sum(p => p.LinesOfCode),
                numLogicalCores = _numLogicalCores,
                systemMemory = _systemMemory,
                SessionId = _sessionId,
                ApplicationId = GetDeploymentHashId(solutionDetail.SolutionFilePath)
            };
        }

        public static ProjectMetrics createProjectMetric(ProjectDetails project, string targetFramework, string version, string source, double analysisTime, string tag, SHA256 sha256hash, DateTime date, string solutionPath, string solutionGuid
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
                language = GetProjectLanguage(project.ProjectFilePath),
                linesOfCode = project.LinesOfCode,
                solutionPath = GetHash(sha256hash, solutionPath),
                SolutionGuid= solutionGuid,
                SessionId = _sessionId,
                ApplicationId = GetDeploymentHashId(solutionPath)
            };
            
        }

        public static NugetMetrics createNugetMetric(string targetFramework, string version, string source, double analysisTime, string tag, DateTime date, string packageId, string packageVersion, Compatibility compatibility, string projectGuid) {
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
                projectGuid = projectGuid,
                SessionId = _sessionId
            };
        }

        public static APIMetrics createAPIMetric(ApiAnalysisResult apiAnalysisResult, string targetFramework, string version, string source, string tag, DateTime date, string projectGuid)
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
                packageVersion = apiAnalysisResult.CodeEntityDetails.Package.Version,
                projectGuid= projectGuid,
                SessionId= _sessionId
            };
        }

        public static void SolutionAssessmentCollect(SolutionAnalysisResult result, string targetFramework, string version, string source, double analysisTime, string tag)
        {
            var date = DateTime.Now;
            var solutionDetail = result.SolutionDetails;
            // Solution Metrics
            var solutionMetrics = createSolutionMetric(solutionDetail, targetFramework, version, source, analysisTime, tag, _sha256hash, date);
            TelemetryCollector.Collect<SolutionMetrics>(solutionMetrics);

            foreach (var project in solutionDetail.Projects)
            {
                var projectMetrics = createProjectMetric(project, targetFramework, version, source, analysisTime, tag, _sha256hash, date, solutionDetail.SolutionFilePath, solutionDetail.SolutionGuid);
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
                    var nugetMetrics = createNugetMetric(targetFramework, version, source, analysisTime, tag, date, packageID, packageVersion, compatability, project.ProjectGuid);
                    TelemetryCollector.Collect<NugetMetrics>(nugetMetrics);
                }

                foreach (var sourceFile in project.SourceFileAnalysisResults)
                {
                    FileAssessmentCollect(sourceFile, targetFramework, version, source, tag, project.ProjectGuid);
                }
            });
        }


        public static void FileAssessmentCollect(SourceFileAnalysisResult result, string targetFramework, string version, string source, string tag, string projectGuid)
        {
            var date = DateTime.Now;
            foreach (var api in result.ApiAnalysisResults)
            {
                var apiMetrics = createAPIMetric(api, targetFramework, version, source, tag, date, projectGuid);
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

        private static string GetDeploymentHashId(string solutionPath, string projectName = "")
        {
            //ID has to be started with letter due to a  restriction for ECR names 
            //current a2c regex  is ^[a - z] +[a - z0 - 9 -] *$, minimum 5 chars maximum 32 chars
            string macId = NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.OperationalStatus == OperationalStatus.Up).Select(nic => nic.GetPhysicalAddress().ToString()).FirstOrDefault();
            if (!String.IsNullOrEmpty(projectName))
            {
                var proj = projectName.ToLower().Length > 10 ? projectName.ToLower().Substring(0, 10) : projectName.ToLower();
                var appId = "d" + proj + GetHash(_sha256hash, macId + solutionPath + projectName);
                return appId.Substring(0, 32);
            }
            else
            {
                var deploymentId = GetHash(_sha256hash, macId + solutionPath);
                return deploymentId.Substring(0, 32);
            }
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
