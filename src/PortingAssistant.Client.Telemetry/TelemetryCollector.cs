﻿using Newtonsoft.Json;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.Telemetry.Model;
using Serilog;
using System;
using System.Security.Cryptography;
using System.Text;
using Serilog.Templates;
using ILogger = Serilog.ILogger;
using System.Linq;
using System.Net.NetworkInformation;
using PortingAssistant.Compatibility.Common.Model;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using CodeEntityType = PortingAssistant.Client.Model.CodeEntityType;
using CompatibilityResult = PortingAssistant.Client.Model.CompatibilityResult;
using Amazon.Runtime.Internal.Transform;
using PortingAssistant.Compatibility.Common.Utils;

namespace PortingAssistantExtensionTelemetry
{
    public static class TelemetryCollector
    {
        private static string _filePath;
        private static ILogger _logger;
        private static ILogger _metricsLogger;
        private static bool _disabledMetrics = false;
        private static int _numLogicalCores;
        private static double _systemMemory;
        private static SHA256 _sha256hash = SHA256.Create();
        private static string _sessionId = Guid.NewGuid().ToString();

        public static void Builder(ILogger logger, string filePath)
        {
            if (_logger == null && _filePath == null)
            {
                _logger = logger;
                _filePath = filePath;
            }
            _metricsLogger = new LoggerConfiguration().
                WriteTo.File(
                                new ExpressionTemplate("{@m}\n"), // Prints {log-message} \n to the file.
                                _filePath,
                                rollingInterval: RollingInterval.Day
                           ).CreateLogger();
            _numLogicalCores = Environment.ProcessorCount;
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            var installedMemory = gcMemoryInfo.TotalAvailableMemoryBytes;
            _systemMemory = (double)installedMemory / 1048576.0;
        }

        public static void Collect<T>(T t)
        {
            _metricsLogger.Information("{t}", JsonConvert.SerializeObject(t));
        }

        public static SolutionMetrics CreateSolutionMetric
            (
                SolutionDetails solutionDetail,
                string targetFramework,
                string version,
                string source,
                double analysisTime,
                string tag,
                SHA256 sha256hash,
                DateTime date
            )
        {
            var solutionMetrics = new SolutionMetrics();
            try
            {
                solutionMetrics.metricsType = MetricsType.solution;
                solutionMetrics.version = version;
                solutionMetrics.portingAssistantSource = source;
                solutionMetrics.tag = tag;
                solutionMetrics.targetFramework = targetFramework;
                solutionMetrics.timeStamp = date.ToString("MM/dd/yyyy HH:mm");
                solutionMetrics.solutionName = GetHash(sha256hash, solutionDetail.SolutionName);
                solutionMetrics.solutionPath = GetHash(sha256hash, solutionDetail.SolutionFilePath);
                solutionMetrics.ApplicationGuid = solutionDetail.ApplicationGuid;
                solutionMetrics.SolutionGuid = solutionDetail.SolutionGuid;
                solutionMetrics.RepositoryUrl = solutionDetail.RepositoryUrl;
                solutionMetrics.analysisTime = analysisTime;
                solutionMetrics.linesOfCode = solutionDetail.Projects.Sum(p => p.LinesOfCode);
                solutionMetrics.numLogicalCores = _numLogicalCores;
                solutionMetrics.systemMemory = _systemMemory;
                solutionMetrics.SessionId = _sessionId;
                solutionMetrics.ApplicationId = GetDeploymentHashId(solutionDetail.SolutionFilePath);
            }
            catch (Exception ex)
            {
                // logging to check what causes solution detail to be null
                _logger.Error("Failed to create solution metric object", ex);
            }
            return solutionMetrics;
        }

        public static ProjectMetrics CreateProjectMetric
            (
                ProjectDetails project,
                string targetFramework,
                string version,
                string source,
                double analysisTime,
                string tag,
                SHA256 sha256hash,
                DateTime date,
                string solutionPath,
                string solutionGuid
            ) {
            var projectMetrics = new ProjectMetrics();
            try
            {
                projectMetrics.metricsType = MetricsType.project;
                projectMetrics.portingAssistantSource = source;
                projectMetrics.tag = tag;
                projectMetrics.version = version;
                projectMetrics.targetFramework = targetFramework;
                projectMetrics.sourceFrameworks = project.TargetFrameworks;
                projectMetrics.timeStamp = date.ToString("MM/dd/yyyy HH:mm");
                projectMetrics.projectName = GetHash(sha256hash, project.ProjectName);
                projectMetrics.projectGuid = project.ProjectGuid;
                projectMetrics.projectType = project.ProjectType;
                projectMetrics.numNugets = project.PackageReferences.Count;
                projectMetrics.numReferences = project.ProjectReferences.Count;
                projectMetrics.isBuildFailed = project.IsBuildFailed;
                projectMetrics.language = GetProjectLanguage(project.ProjectFilePath);
                projectMetrics.linesOfCode = project.LinesOfCode;
                projectMetrics.solutionPath = GetHash(sha256hash, solutionPath);
                projectMetrics.SolutionGuid= solutionGuid;
                projectMetrics.SessionId = _sessionId;
                projectMetrics.ApplicationId = GetDeploymentHashId(solutionPath);
            }
            catch(Exception ex)
            {
                // logging to check what causes project detail to be null
                _logger.Error("Failed to create project metric object", ex);
            }
            return projectMetrics;

        }

        public static NugetMetrics CreateNugetMetric
            (
                string targetFramework,
                string version,
                string source,
                double analysisTime,
                string tag,
                DateTime date,
                string packageId,
                string packageVersion,
                PortingAssistant.Client.Model.Compatibility compatibility,
                string projectGuid,
                string solutionGuid,
                string? accountId = null
            ) {
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
                solutionGuid = solutionGuid,
                accountId = accountId,
                SessionId = _sessionId
            };
        }

        public static APIMetrics CreateAPIMetric
            (
                ApiAnalysisResult apiAnalysisResult,
                string targetFramework,
                string version,
                string source,
                string tag,
                DateTime date,
                string projectGuid,
                string solutionGuid,
                string? accountId = null
            )
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
                projectGuid = projectGuid,
                solutionGuid = solutionGuid,
                accountId = accountId,
                SessionId = _sessionId
            };
        }

        public static void SolutionAssessmentCollect(
                SolutionAnalysisResult result,
                string targetFramework,
                string version,
                string source,
                double analysisTime,
                string tag
            )
        {
            if (_disabledMetrics) { return; }

            var date = DateTime.Now;
            var solutionDetail = result.SolutionDetails;
            // Solution Metrics
            var solutionMetrics = CreateSolutionMetric(solutionDetail, targetFramework, version, source, analysisTime, tag, _sha256hash, date);
            TelemetryCollector.Collect<SolutionMetrics>(solutionMetrics);

            foreach (var project in solutionDetail.Projects)
            {
                var projectMetrics = CreateProjectMetric(project, targetFramework, version, source, analysisTime, tag, _sha256hash, date, solutionDetail.SolutionFilePath, solutionDetail.SolutionGuid);
                TelemetryCollector.Collect<ProjectMetrics>(projectMetrics);
            }

            //nuget metrics
            result.ProjectAnalysisResults.ForEach(project =>
            {
                
                foreach (var nuget in project.PackageAnalysisResults)
                {
                    PortingAssistant.Client.Model.CompatibilityResult defaultCompatibilityResult = new PortingAssistant.Client.Model.CompatibilityResult()
                    {
                        Compatibility = PortingAssistant.Client.Model.Compatibility.UNKNOWN,
                        CompatibleVersions = new System.Collections.Generic.List<string>()
                    };

                    nuget.Value.Wait();
                    var packageID = nuget.Value.Result.PackageVersionPair.PackageId;
                    var packageVersion = nuget.Value.Result.PackageVersionPair.Version;
                    PortingAssistant.Client.Model.CompatibilityResult compatability;
                    if (!nuget.Value.Result.CompatibilityResults.TryGetValue(targetFramework, out compatability))
                    {
                        compatability = defaultCompatibilityResult;
                    }
                    
                    var nugetMetrics = CreateNugetMetric(targetFramework, version,
                        source, analysisTime, tag, date, packageID, packageVersion,
                        compatability.Compatibility, project.ProjectGuid,
                        solutionDetail.SolutionGuid);
                    TelemetryCollector.Collect<NugetMetrics>(nugetMetrics);
                }

                foreach (var sourceFile in project.SourceFileAnalysisResults)
                {
                    FileAssessmentCollect(sourceFile, targetFramework, version, source, tag, project.ProjectGuid, solutionDetail.SolutionGuid);
                }
            });
        }


        public static void FileAssessmentCollect
            (
                SourceFileAnalysisResult result,
                string targetFramework,
                string version,
                string source,
                string tag,
                string projectGuid,
                string solutionGuid
            )
        {
            if (_disabledMetrics) { return; }

            var date = DateTime.Now;
            foreach (var api in result.ApiAnalysisResults)
            {
                var apiMetrics = CreateAPIMetric(api, targetFramework, version, source, tag, date, projectGuid, solutionGuid);
                TelemetryCollector.Collect<APIMetrics>(apiMetrics);
            }
        }

        public static void DllAssessmentCollect(CompatibilityCheckerResponse result, string targetFramework, string version, string source, double analysisTime, string tag, string accountId)
        {
            var date = DateTime.Now;
            var metrics = new ArrayList();
            var count = 0;

            // nuget metrics
            foreach (var nuget in result.PackageAnalysisResults)
            {
                var packageID = nuget.Key.PackageId;
                var packageVersion = nuget.Key.Version;
                PortingAssistant.Client.Model.Compatibility compatibility = (PortingAssistant.Client.Model.Compatibility)nuget.Value.CompatibilityResults[targetFramework].Compatibility;
                var nugetMetrics = CreateNugetMetric(targetFramework, version, source, analysisTime, tag, date, packageID, packageVersion, compatibility, null, null, accountId);
                metrics.Add(nugetMetrics);
                count++;
                if (count >= 2000)
                {
                    Collect(metrics);
                    metrics.Clear();
                    count = 0;
                }
            }

            //API metrics
            var apiAnalysisResults = result.ApiAnalysisResults;
            foreach (var apiResult in apiAnalysisResults)
            {
                var packageVersionPair = apiResult.Key;
                foreach (var analysisResultPair in apiResult.Value)
                {
                    var apiAnalysisResult = new ApiAnalysisResult
                    {
                        CodeEntityDetails = new CodeEntityDetails
                        {
                            Package = new PortingAssistant.Client.Model.PackageVersionPair
                            {
                                PackageId = packageVersionPair.PackageId,
                                Version = packageVersionPair.Version,
                                PackageSourceType = (PortingAssistant.Client.Model.PackageSourceType)packageVersionPair.PackageSourceType
                            },
                            CodeEntityType = CodeEntityType.Method,
                            OriginalDefinition = analysisResultPair.Key,
                            Namespace = packageVersionPair.PackageId
                        },
                        CompatibilityResults = analysisResultPair.Value.CompatibilityResults
                            .ToDictionary(kvp => kvp.Key, kvp => new CompatibilityResult
                            {
                                Compatibility = (PortingAssistant.Client.Model.Compatibility)kvp.Value.Compatibility,
                                CompatibleVersions = kvp.Value.CompatibleVersions
                            })
                    };
                    var apiMetrics = CreateAPIMetric(apiAnalysisResult, targetFramework, version, source, tag, date, null, null, accountId);
                    metrics.Add(apiMetrics);
                    count++;
                    if (count >= 2000)
                    {
                        Collect(metrics);
                        metrics.Clear();
                        count = 0;
                    }
                }
            }
            Collect(metrics);
        }

        public static void ToggleMetrics(bool disabledMetrics)
        {
            _disabledMetrics = disabledMetrics;
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

        private static string GetDeploymentHashId(string solutionPath)
        {
            string macId = NetworkInterface.GetAllNetworkInterfaces().Where
                (
                    nic => nic.OperationalStatus == OperationalStatus.Up
                ).
                Select
                (
                    nic => nic.GetPhysicalAddress().ToString()
                ).
                FirstOrDefault();
            var deploymentId = GetHash(_sha256hash, macId + solutionPath);
            return deploymentId?.Length > 32 ? deploymentId.Substring(0, 32) : deploymentId;

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
