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
                string FileName = portingResult.ProjectFile + "-porting-result.json";
                string FileDir = Path.Combine(outputFolder, SolutionName + AnalyzeRootFolder, PortingResultFolder, FileName);
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
                    Directory.CreateDirectory(FileDir);
                    List<PackageAnalysisResult> packageAnalysisResults = new List<PackageAnalysisResult>();
                    Dictionary<PackageVersionPair, string> packageAnalysisResultErrors = new Dictionary<PackageVersionPair, string>();

                    projectAnalysResult.PackageAnalysisResults.ToList()
                    .ForEach(p =>
                    {
                        p.Value.ContinueWith(result =>
                        {
                            if (result.IsCompletedSuccessfully)
                            {
                                packageAnalysisResults.Add(result.Result);
                            }
                            else
                            {
                                packageAnalysisResultErrors.Add(p.Key, result.Exception.Message);
                            }
                        });
                    });

                    //project apis analsis result
                    string ApiAnalyzeFileName = ProjectName + "-api-analysis.json";
                    var projectApiAnalysisResult = projectAnalysResult.IsBuildFailed ? new ProjectApiAnalysisResult
                    {
                        Errors = new List<string> { $"Errors during compilation in {projectAnalysResult.ProjectName}." },
                        SolutionFile = SolutionName,
                        ProjectFile = ProjectName,
                    } : new ProjectApiAnalysisResult
                    {
                        Errors = projectAnalysResult.Errors,
                        SolutionFile = SolutionName,
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
                if (FailedProjects != null && FailedProjects.Count != 0)
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
                using (var destinationStream = new FileStream(FilePath, FileMode.Create))
                {
                    using (var memoStream = new MemoryStream(obj.Serialize<T>()))
                    {
                        await memoStream.CopyToAsync(destinationStream);
                    }
                }
                _logger.LogInformation("file generated at ", FilePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("failed to generate report: {0}", ex);
                return false;
            }
        }
    }

    public static class DataExtensions
    {

        public static JsonSerializerSettings JsonSettings { get; } = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        public static JsonSerializer Serializer { get; } = JsonSerializer.Create(JsonSettings);

        public static byte[] Serialize<T>(this T data)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var writer = new StreamWriter(outputStream))
                {
                    using (var jsonWriter = new JsonTextWriter(writer))
                    {
                        Serializer.Serialize(jsonWriter, data);
                    }
                }

                return outputStream.ToArray();
            }
        }

        public static T Deserialize<T>(this Stream stream)
        {
            var reader = new StreamReader(stream);

            return (T)Serializer.Deserialize(reader, typeof(T));
        }
    }
}
