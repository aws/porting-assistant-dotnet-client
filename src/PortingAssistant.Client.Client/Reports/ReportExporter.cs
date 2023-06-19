using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Client.Reports
{
    public class ReportExporter : IReportExporter
    {
        private readonly ILogger _logger;
        private readonly string PortingResultFolder = "porting";
        private readonly string SolutionAnalyzeFolder = "solution-analyze";
        private readonly string AnalyzeRootFolder = "-analyze";
        public ReportExporter(ILogger<ReportExporter> logger)
        {
            _logger = logger;
        }

        public bool GenerateJsonReport(
            List<PortingResult> portingResults,
            string SolutionName, string outputFolder)
        {
            portingResults.ForEach(portingResult =>
            {
                string FileName =Path.GetFileName(portingResult.ProjectFile) + "-porting-result.json";
                string FileDir = Path.Combine(outputFolder, SolutionName + AnalyzeRootFolder, PortingResultFolder, FileName);
                if (Directory.Exists(FileDir))
                {
                    Directory.Delete(FileDir, true);
                }
                Directory.CreateDirectory(FileDir);
                var writeToFile = WriteReportToFileAsync(portingResult, Path.Combine(FileDir, FileName));
                writeToFile.Wait();
            });
            return true;
        }

        public bool GenerateJsonReport(
            SolutionAnalysisResult solutionAnalysisResult,
            string outputFolder)
        {
            try
            {
                string SolutionName = solutionAnalysisResult.SolutionDetails.SolutionName;
                string BaseDir = Path.Combine(outputFolder, SolutionName + AnalyzeRootFolder, SolutionAnalyzeFolder);
                Dictionary<string, string> FailedProjects = new Dictionary<string, string>();

                solutionAnalysisResult.ProjectAnalysisResults.ForEach(projectAnalysResult =>
                {
                    if (projectAnalysResult == null)
                    {
                        return;
                    }
                    List<Task<bool>> writeToFiles = new List<Task<bool>>();
                    string ProjectName = projectAnalysResult.ProjectName;
                    string FileDir = Path.Combine(BaseDir, ProjectName);
                    if (Directory.Exists(FileDir))
                    {
                        Directory.Delete(FileDir, true);
                    }
                    Directory.CreateDirectory(FileDir);
                    List<PackageAnalysisResult> packageAnalysisResults = new List<PackageAnalysisResult>();
                    Dictionary<PackageVersionPair, string> packageAnalysisResultErrors = new Dictionary<PackageVersionPair, string>();

                    projectAnalysResult.PackageAnalysisResults.ToList()
                    .ForEach(p =>
                    {
                        if (p.Value.IsCompletedSuccessfully)
                        {
                            packageAnalysisResults.Add(p.Value.Result);
                        }
                        else
                        {
                            packageAnalysisResultErrors.Add(p.Key, p.Value.Exception.Message);
                        };
                    });

                    //project apis analsis result
                    string ApiAnalyzeFileName = ProjectName + "-api-analysis.json";
                    var projectApiAnalysisResult = projectAnalysResult.IsBuildFailed ? new ProjectApiAnalysisResult
                    {
                        Errors = new List<string> { $"Errors during compilation in {projectAnalysResult.ProjectName}." },
                        SchemaVersion = Common.Model.Schema.version,
                        SolutionFile = SolutionName,
                        SolutionGuid = solutionAnalysisResult.SolutionDetails.SolutionGuid,
                        ApplicationGuid = solutionAnalysisResult.SolutionDetails.ApplicationGuid,
                        RepositoryUrl = solutionAnalysisResult.SolutionDetails.RepositoryUrl,
                        ProjectFile = ProjectName,
                    } : new ProjectApiAnalysisResult
                    {
                        Errors = projectAnalysResult.Errors,
                        SchemaVersion = Common.Model.Schema.version,
                        SolutionFile = SolutionName,
                        SolutionGuid = solutionAnalysisResult.SolutionDetails.SolutionGuid,
                        ApplicationGuid = solutionAnalysisResult.SolutionDetails.ApplicationGuid,
                        RepositoryUrl = solutionAnalysisResult.SolutionDetails.RepositoryUrl,
                        ProjectFile = ProjectName,
                        SourceFileAnalysisResults = projectAnalysResult.SourceFileAnalysisResults
                    };
                    writeToFiles.Add(WriteReportToFileAsync(projectApiAnalysisResult, Path.Combine(FileDir, ApiAnalyzeFileName)));

                    //project packages analsis result
                    string PackageAnalyzeFileName = ProjectName + "-package-analysis.json";
                    writeToFiles.Add(WriteReportToFileAsync(packageAnalysisResults, Path.Combine(FileDir, PackageAnalyzeFileName)));

                    //project failed packages result
                    if (packageAnalysisResultErrors != null && packageAnalysisResultErrors.Count != 0)
                    {
                        string PackageAnalyzeErrorFileName = ProjectName + "-package-analysis-error.json";
                        writeToFiles.Add(WriteReportToFileAsync(packageAnalysisResults, Path.Combine(FileDir, PackageAnalyzeErrorFileName)));
                    }
                    Task.WaitAll(writeToFiles.ToArray());

                });
                if (FailedProjects?.Count != 0)
                {
                    WriteReportToFileAsync(FailedProjects, Path.Combine(BaseDir, "failed.json")).Wait();
                }
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError("failed to generate analyze report: {0}", ex);
                return false;
            }
        }

        private async Task<bool> WriteReportToFileAsync<T>(T obj, string FilePath)
        {
            try
            {
                await File.AppendAllTextAsync(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                _logger.LogInformation("file generated at: {0}", FilePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("failed to generate report: {0}", ex);
                return false;
            }
        }
    }
}
