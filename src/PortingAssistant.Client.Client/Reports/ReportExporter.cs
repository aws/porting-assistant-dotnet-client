using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using PortingAssistant.Client.Model;
using Google.Protobuf;
using com.amazon.awsassessment.analysis.v2;
using static com.amazon.awsassessment.analysis.v2.SourceCodeAnalyzerOutputV2.Types;
using static com.amazon.awsassessment.analysis.v2.AntipatternInstance.Types;
using static com.amazon.awsassessment.analysis.v2.SourceCodeAnalyzerOutputV2.Types.SourceCodeAnalyzerOutputEntry.Types;
using com.amazon.awsassessment.analysis.io;

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

        public bool GenerateProtoReport(SolutionAnalysisResult solutionAnalysisResult, string outputPath)
        {

            SourceCodeAnalyzerOutputV2 outputV2 = new SourceCodeAnalyzerOutputV2
            {
                Id = new AnalyzerId { Value = "PortingAssistant" },
                Status = Status.Failure,
            };

            var output = new SourceCodeAnalyzerOutput();

            var analysisSummary = new SourceCodeAnalyzerSummary
            {
                Language = ProgrammingLanguage.Csharp,
                NumberOfClasses = 0,
                NumberOfFiles = 0,
                NumberOfImports = 0,
                NumberOfLines = 0,
                NumberOfMethods = 0,
            };
            outputV2.Summary = analysisSummary;

            try
            {
                var package_instance = new AntipatternInstance
                {
                    AntipatternName = "Nuget Package",
                    AntipatternType = AntipatternType.SourceCodeAnalysisAntipattern,
                };

                var api_instance = new AntipatternInstance
                {
                    AntipatternName = "API Compatibility",
                    AntipatternType = AntipatternType.SourceCodeAnalysisAntipattern
                };


                var actions_instance = new AntipatternInstance
                {
                    AntipatternName = "Recommended Actions",
                    AntipatternType = AntipatternType.SourceCodeAnalysisAntipattern
                };

                solutionAnalysisResult.ProjectAnalysisResults.ForEach(projectAnalysResult =>
                {
                    if (projectAnalysResult == null) return;

                    var projectMetaData = new SourceCodeAnalyzerOutputEntry
                    {
                        Key = SourceCodeAnalyzerOutputKey.DefaultSourceCodeAnalyzerOutputKey,
                        Type = ObjectType.Map,
                        Name = projectAnalysResult.ProjectName,
                        Value = $"projectGuid:{projectAnalysResult.ProjectGuid}," +
                        $"ProjectFilePath:{projectAnalysResult.ProjectFilePath}," +
                        $"ProjectType:{projectAnalysResult.ProjectType}," +
                        $"TargetFrameworks:{string.Join(", ", projectAnalysResult.TargetFrameworks)}," +
                        $"ProjectReferences:{string.Join(", ", projectAnalysResult.ProjectReferences)}",
                        Delimiter = ",",
                    };

                    outputV2.MetadataEntries.Add(projectMetaData);

                    projectAnalysResult.PackageAnalysisResults.ToList().ForEach(async p =>
                    {
                        analysisSummary.NumberOfImports++;

                        var packageAnalysisResult = await p.Value;
                        var compatibility = packageAnalysisResult.CompatibilityResults.GetValueOrDefault("netcoreapp3.1", null);

                        var AntipatternDetails = new AntipatternDetails
                        {
                            AntipatternLocation = new AntipatternLocation
                            {
                                ResourceName = packageAnalysisResult.PackageVersionPair.PackageId + "-" + packageAnalysisResult.PackageVersionPair.Version
                            },
                            Severity = compatibility == null ? Severity.Critical : compatibility.Compatibility == Compatibility.COMPATIBLE ? Severity.Low : Severity.Critical,
                            Recommendation = new Recommendation
                            {
                                Description = compatibility.CompatibleVersions.Count != 0 ? "upgrade package version to " + String.Join(", ", compatibility.CompatibleVersions) : "",
                            }
                        };

                        package_instance.AntipatternDetails.Add(AntipatternDetails);
                    });

                    projectAnalysResult.SourceFileAnalysisResults.ForEach(sourceFileAnalysisResult =>
                    {
                        analysisSummary.NumberOfFiles++;
                        sourceFileAnalysisResult.ApiAnalysisResults.ForEach(api =>
                        {
                            analysisSummary.NumberOfMethods++;
                            var compatibility = api.CompatibilityResults.GetValueOrDefault("netcoreapp3.1", null);

                            var name = api.CodeEntityDetails.Signature;
                            var package = api.CodeEntityDetails.Package;
                            var rcommnadation = api.Recommendations.RecommendedActions
                                .Where(r => r.RecommendedActionType != RecommendedActionType.NoRecommendation)
                                .Select(r =>
                                {
                                    switch (r.RecommendedActionType)
                                    {
                                        case RecommendedActionType.UpgradePackage:
                                            return $"Upgrade Source Package { package.PackageId}-{package.Version} to version " + r.Description;
                                        case RecommendedActionType.ReplacePackage:
                                            return $"Replace Source Package { package.PackageId}-{package.Version} with " + r.Description;
                                        case RecommendedActionType.ReplaceApi:
                                            return "Replace API with " + r.Description;
                                        case RecommendedActionType.ReplaceNamespace:
                                            return "Replace namespace with " + r.Description;
                                        case RecommendedActionType.NoRecommendation:
                                            break;
                                        default:
                                            break;
                                    }
                                    return "";
                                });
                            var targetfrmawork = "netcoreapp3.1";
                            var message = $"{name} is incompatible for target framework {targetfrmawork} " + String.Join(", ", rcommnadation);
                            var AntipatternDetails = new AntipatternDetails
                            {
                                AntipatternLocation = new AntipatternLocation
                                {
                                    ResourceName = api.CodeEntityDetails.Signature,
                                    StartLinePosition = (int)api.CodeEntityDetails.TextSpan.StartLinePosition,
                                    StartCharPosition = (int)api.CodeEntityDetails.TextSpan.StartCharPosition,
                                    EndLinePosition = (int)api.CodeEntityDetails.TextSpan.EndLinePosition,
                                    EndCharPosition = (int)api.CodeEntityDetails.TextSpan.EndCharPosition,
                                },
                                Severity = compatibility == null ? Severity.Critical : compatibility.Compatibility == Compatibility.COMPATIBLE ? Severity.Low : Severity.Critical,
                                Recommendation = new Recommendation
                                {
                                    Description = message
                                }
                            };

                            api_instance.AntipatternDetails.Add(AntipatternDetails);
                        });

                        sourceFileAnalysisResult.RecommendedActions.ForEach(action =>
                        {
                            var AntipatternDetails = new AntipatternDetails
                            {
                                AntipatternLocation = new AntipatternLocation
                                {
                                    ResourceName = "Porting Actions",
                                    StartLinePosition = (int)action.TextSpan.StartLinePosition,
                                    StartCharPosition = (int)action.TextSpan.StartCharPosition,
                                    EndLinePosition = (int)action.TextSpan.EndLinePosition,
                                    EndCharPosition = (int)action.TextSpan.EndCharPosition,
                                },
                                Severity = Severity.Medium,
                                Recommendation = new Recommendation
                                {
                                    Description = action.Description
                                }
                            };
                            actions_instance.AntipatternDetails.Add(AntipatternDetails);
                        });

                    });
                });

                outputV2.Instances.Add(package_instance);
                outputV2.Instances.Add(api_instance);
                outputV2.Instances.Add(actions_instance);

                using (var outfile = File.Create(outputPath))
                {
                    outputV2.Status = Status.Success;
                    output.OutputV2 = outputV2;
                    output.WriteTo(outfile);
                }

                return true;
            }
            catch
            {
                using (var outfile = File.Create(outputPath))
                {
                    outputV2.Status = Status.Failure;
                    output.OutputV2 = outputV2;
                    output.WriteTo(outfile);
                }
                return false;
            }
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
                await File.WriteAllTextAsync(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
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
}
