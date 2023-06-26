using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using PortingAssistant.Client.Analysis;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.Porting;
using PortingAssistant.Client.Client.FileParser;
using NuGet.Frameworks;
using NuGet.Versioning;
using PortingAssistant.Client.PortingProjectFile;
using CTA.Rules.Models;

namespace PortingAssistant.Client.Tests
{
    public class PortingAssistantHandlerTest
    {
        private Mock<IPortingAssistantAnalysisHandler> _apiAnalysisHandlerMock;
        private IPortingHandler _portingHandlerMock;
        private PortingAssistantClient _portingAssistantClient;
        private readonly string _solutionFolder = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithProjects");
        private string _tmpDirectory;
        private string _tmpProjectPath;
        private string _tmpSolutionDirectory;
        private string _tmpSolutionFileName;
        private static string DEFAULT_TARGET = "net6.0";

        private readonly PackageDetails _packageDetails = new PackageDetails
        {
            Name = "Newtonsoft.Json",
            Versions = new SortedSet<string> { "12.0.3", "12.0.4" },
            Api = new ApiDetails[]
            {
                new ApiDetails
                {
                    MethodName = "Setup(Object)",
                    MethodSignature = "Accessibility.Setup(Object)",
                    Targets = new Dictionary<string, SortedSet<string>>
                    {
                        {
                             "netcoreapp3.1", new SortedSet<string> { "12.0.3", "12.0.4" }
                        },
                        {
                             "net6.0", new SortedSet<string> { "12.0.3", "12.0.4" }
                        }
                    },
                }
            },
            Targets = new Dictionary<string, SortedSet<string>> {
                {
                    "netcoreapp3.1",
                    new SortedSet<string> { "12.0.3", "12.0.4" }
                },
                {
                    "net6.0",
                    new SortedSet<string> { "12.0.3", "12.0.4" }
                }
            },
            License = new LicenseDetails
            {
                License = new Dictionary<string, SortedSet<string>>
                {
                    { "MIT", new SortedSet<string> { "12.0.3", "12.0.4" } }
                }
            }
        };

        private readonly SourceFileAnalysisResult _sourceFileAnalysisResult = new SourceFileAnalysisResult
        {
            SourceFileName = "test",
            SourceFilePath = "/test/test",
            ApiAnalysisResults = new List<ApiAnalysisResult>
            {
                new ApiAnalysisResult
                {
                    CompatibilityResults = new Dictionary<string, CompatibilityResult>
                    {
                        { DEFAULT_TARGET, new CompatibilityResult{
                            Compatibility = Compatibility.COMPATIBLE,
                            CompatibleVersions = new List<string>{ "12.0.3", "12.0.4" }
                        } }
                    }
                }
            }
        };

        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (var fileInfo in files)
            {
                string tempPath = Path.Combine(destDirName, fileInfo.Name);
                fileInfo.CopyTo(tempPath, true);
            }

            if (copySubDirs)
            {
                foreach (var subDir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subDir.Name);
                    DirectoryCopy(subDir.FullName, tempPath, true);
                }
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _apiAnalysisHandlerMock = new Mock<IPortingAssistantAnalysisHandler>();
            _portingHandlerMock = new PortingHandler(new PortingProjectFileHandler(NullLogger<PortingProjectFileHandler>.Instance));
            _portingAssistantClient = new PortingAssistantClient(
                _apiAnalysisHandlerMock.Object,
                _portingHandlerMock);
        }

        [SetUp]
        public void SetUp()
        {
            var solutionDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestXml", "TestPorting");
            _tmpDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestXml", "TmpDirectory");
            DirectoryCopy(solutionDirectory, _tmpDirectory, true);

            _tmpSolutionDirectory = Path.Combine(_tmpDirectory, "src");
            _tmpSolutionFileName = Path.Combine(_tmpSolutionDirectory, "NopCommerce.sln");
            _tmpProjectPath = Path.Combine(_tmpSolutionDirectory, "Libraries", "Nop.Core", "Nop.Core.csproj");

            _apiAnalysisHandlerMock.Reset();
            _apiAnalysisHandlerMock.Setup(analyzer => analyzer.AnalyzeSolution(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<AnalyzerSettings>()))
                .Returns((string solutionFilePath, List<string> projects, string targetFramework, AnalyzerSettings analyzerSettings) =>
                {
                    return Task.Run(() => projects.Select(project =>
                    {
                        var package = new PackageVersionPair
                        {
                            PackageId = "Newtonsoft.Json",
                            Version = "11.0.1"
                        };
                        var packageAnalysisResult = Task.Run(() => new PackageAnalysisResult
                        {
                            PackageVersionPair = package,
                            CompatibilityResults = new Dictionary<string, CompatibilityResult>
                            {
                                {targetFramework, new CompatibilityResult{
                                    Compatibility = Compatibility.COMPATIBLE,
                                    CompatibleVersions = new List<string>
                                    {
                                        "12.0.3", "12.0.4"
                                    }
                                }}
                            },
                            Recommendations = new PortingAssistant.Client.Model.Recommendations
                            {
                                RecommendedActions = new List<RecommendedAction>
                                {
                                    new RecommendedAction
                                    {
                                        RecommendedActionType = RecommendedActionType.UpgradePackage,
                                        Description = "12.0.3"
                                    }
                                }
                            }
                        });

                        var projectAnalysisResult = new ProjectAnalysisResult
                        {
                            ProjectName = Path.GetFileNameWithoutExtension(project),
                            ProjectFilePath = project,
                            PackageAnalysisResults = new Dictionary<PackageVersionPair, Task<PackageAnalysisResult>>
                            {
                                { package, packageAnalysisResult }
                            },
                            SourceFileAnalysisResults = new List<SourceFileAnalysisResult>
                            {
                                _sourceFileAnalysisResult
                            },
                            ProjectGuid = "xxx",
                            ProjectType = nameof(SolutionProjectType.KnownToBeMSBuildFormat),
                            PreportMetaReferences = new List<string> { },
                            MetaReferences = new List<string> { },
                            ExternalReferences = null,
                            ProjectRules = null
                        };

                        return new KeyValuePair<string, ProjectAnalysisResult>(project, projectAnalysisResult);
                    }).ToDictionary(k => k.Key, v => v.Value));
                });
            _apiAnalysisHandlerMock.Setup(analyzer => analyzer.AnalyzeSolutionIncremental(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<AnalyzerSettings>()))
                .Returns((string solutionFilePath, List<string> projects, string targetFramework, AnalyzerSettings analyzerSettings) =>
                {
                    return Task.Run(() =>
                    {
                        return projects.Select(project =>
                        {
                            var package = new PackageVersionPair
                            {
                                PackageId = "Newtonsoft.Json",
                                Version = "11.0.1"
                            };
                            var packageAnalysisResult = Task.Run(() => new PackageAnalysisResult
                            {
                                PackageVersionPair = package,
                                CompatibilityResults = new Dictionary<string, CompatibilityResult>
                                {
                                    {targetFramework, new CompatibilityResult{
                                        Compatibility = Compatibility.COMPATIBLE,
                                        CompatibleVersions = new List<string>
                                        {
                                            "12.0.3", "12.0.4"
                                        }
                                    }}
                                },
                                Recommendations = new PortingAssistant.Client.Model.Recommendations
                                {
                                    RecommendedActions = new List<RecommendedAction>
                                    {
                                        new RecommendedAction
                                        {
                                            RecommendedActionType = RecommendedActionType.UpgradePackage,
                                            Description = "12.0.3"
                                        }
                                    }
                                }
                            });

                            var projectAnalysisResult = new ProjectAnalysisResult
                            {
                                ProjectName = Path.GetFileNameWithoutExtension(project),
                                ProjectFilePath = project,
                                PackageAnalysisResults = new Dictionary<PackageVersionPair, Task<PackageAnalysisResult>>
                                {
                                    { package, packageAnalysisResult }
                                },
                                SourceFileAnalysisResults = new List<SourceFileAnalysisResult>
                                {
                                    _sourceFileAnalysisResult
                                },
                                ProjectGuid = "xxx",
                                ProjectType = nameof(SolutionProjectType.KnownToBeMSBuildFormat)
                            };

                            return new KeyValuePair<string, ProjectAnalysisResult>(project, projectAnalysisResult);
                        }).ToDictionary(k => k.Key, v => v.Value);
                    });
                });
            _apiAnalysisHandlerMock.Setup(analyzer => analyzer.AnalyzeFileIncremental(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), 
                It.IsAny<List<string>>(), It.IsAny<RootNodes>(), It.IsAny<Codelyzer.Analysis.Model.ExternalReferences>(), 
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<string>()))
                .Returns((string filePath, string project, string solutionPath, List<string> preportReferences,
                List<string> metaReferences, RootNodes projectRules, Codelyzer.Analysis.Model.ExternalReferences externalReferences, bool actionsOnly, bool compatibleOnly, string targetFramework) =>
                {
                    return Task.Run(() =>
                    {
                        return new List<SourceFileAnalysisResult> { _sourceFileAnalysisResult };
                    });
                });
            _apiAnalysisHandlerMock.Setup(analyzer => analyzer.AnalyzeFileIncremental(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<List<string>>(), It.IsAny<RootNodes>(), It.IsAny<Codelyzer.Analysis.Model.ExternalReferences>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<string>()))
                .Returns((string filePath, string fileContents, string project, string solutionPath, List<string> preportReferences,
                List<string> metaReferences, RootNodes projectRules, Codelyzer.Analysis.Model.ExternalReferences externalReferences, bool actionsOnly, bool compatibleOnly, string targetFramework) =>
                {
                    return Task.Run(() =>
                    {
                        return new List<SourceFileAnalysisResult> { _sourceFileAnalysisResult };
                    });
                });
        }

        private List<ProjectDetails> GetProjects(string pathToSolution)
        {
            var solution = SolutionFile.Parse(pathToSolution);

            var projects = solution.ProjectsInOrder.Select(p =>
            {
                if (p.ProjectType != SolutionProjectType.KnownToBeMSBuildFormat && p.ProjectType != SolutionProjectType.WebProject)
                {
                    return null;
                }

                var projectParser = new ProjectFileParser(p.AbsolutePath);

                return new ProjectDetails
                {
                    ProjectName = p.ProjectName,
                    ProjectFilePath = p.AbsolutePath,
                    ProjectGuid = p.ProjectGuid,
                    TargetFrameworks = projectParser.GetTargetFrameworks().ConvertAll(tfm =>
                    {
                        var framework = NuGetFramework.Parse(tfm);
                        return string.Format("{0} {1}", framework.Framework, NuGetVersion.Parse(framework.Version.ToString()).ToNormalizedString());
                    }),
                    PackageReferences = projectParser.GetPackageReferences()
                };
            }).Where(p => p != null).ToList();

            return projects;
        }

        [TearDown]
        public void Cleanup()
        {
            Directory.Delete(_tmpDirectory, true);
        }

        [Test]
        public void AnalyzeSolutionWithProjectsSucceeds()
        {
            var results = _portingAssistantClient.AnalyzeSolutionAsync(Path.Combine(_solutionFolder, "SolutionWithProjects.sln"), new AnalyzerSettings { TargetFramework = "net6.0" });
            results.Wait();
            var projectAnalysisResult = results.Result.ProjectAnalysisResults.Find(p => p.ProjectName == "Nop.Core");
            var sourceFileAnalysisResults = projectAnalysisResult.SourceFileAnalysisResults;
            var packageAnalysisResult = projectAnalysisResult.PackageAnalysisResults;

            Assert.AreEqual(_sourceFileAnalysisResult, sourceFileAnalysisResults.First());

            Task.WaitAll(packageAnalysisResult.Values.ToArray());
            var packageResult = packageAnalysisResult.First(p => p.Value.Result.PackageVersionPair.PackageId == _packageDetails.Name);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageResult.Value.Result.Recommendations.RecommendedActions.First().RecommendedActionType);
            var compatibilityResult = packageResult.Value.Result.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET);
            Assert.AreEqual(Compatibility.COMPATIBLE, compatibilityResult.Compatibility);
            Assert.AreEqual("12.0.3", compatibilityResult.CompatibleVersions.First());

            var solutionDetail = results.Result.SolutionDetails;
            Assert.AreEqual("SolutionWithProjects", solutionDetail.SolutionName);

            Assert.AreEqual(5, solutionDetail.Projects.Count);
            Assert.Contains("PortingAssistantApi", solutionDetail.Projects.ConvertAll(result => result.ProjectName));

            var project = solutionDetail.Projects.First(project => project.ProjectName.Equals("PortingAssistantApi"));
            Assert.AreEqual("xxx", project.ProjectGuid);
            Assert.AreEqual(nameof(SolutionProjectType.KnownToBeMSBuildFormat), project.ProjectType);
        }

        [Test]
        public void PortProjectFile()
        {
            var request = new PortingRequest
            {
                Projects = new List<ProjectDetails> {
                    new ProjectDetails {
                        ProjectFilePath = _tmpProjectPath,
                        PackageReferences = new List<PackageVersionPair>
                        {
                            new PackageVersionPair
                            {
                                PackageId = "Newtonsoft.Json",
                                Version = "9.0.1"
                            }
                        }
                    }
                },
                SolutionPath = _tmpSolutionFileName,
                TargetFramework = "netcoreapp3.1",
                RecommendedActions = new List<RecommendedAction>
                {
                    new PackageRecommendation
                    {
                        PackageId = "Newtonsoft.Json",
                        Version = "9.0.1",
                        RecommendedActionType = RecommendedActionType.UpgradePackage,
                        TargetVersions = new List<string> { "13.0.1" },
                    }
                }
            };

            var result = _portingAssistantClient.ApplyPortingChanges(request);
            Assert.True(result[0].Success);
            Assert.AreEqual(_tmpProjectPath, result[0].ProjectFile);
            Assert.AreEqual("Nop.Core", result[0].ProjectName);

            var portResult = GetProjects(Path.Combine(_tmpSolutionDirectory, "NopCommerce.sln")).Find(package => package.ProjectName == "Nop.Core");
            Assert.AreEqual(_tmpProjectPath, portResult.ProjectFilePath);
            Assert.AreEqual(".NETCoreApp 3.1.0", portResult.TargetFrameworks[0]);
            Assert.AreEqual(
                new PackageVersionPair
                {
                    PackageId = "Newtonsoft.Json",
                    Version = "13.0.1"
                },
                portResult.PackageReferences.Find(nugetPackage => nugetPackage.PackageId == "Newtonsoft.Json"));
        }


        [Test]
        public void GetProjectWithCorruptedSolutionFileThrowsException()
        {
            var testSolutionPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithFailedContent", "NopCommerce.sln");

            Assert.Throws<AggregateException>(() =>
            {
                var result = _portingAssistantClient.AnalyzeSolutionAsync(testSolutionPath, new AnalyzerSettings());
                result.Wait();
            });
        }

        [Test]
        public void AnalyzeFileSucceedsTest()
        {
            var results = _portingAssistantClient.AnalyzeSolutionAsync(Path.Combine(_solutionFolder, "SolutionWithProjects.sln"), new AnalyzerSettings { TargetFramework = "netcoreapp3.1", ContiniousEnabled = true, CompatibleOnly = true });
            results.Wait();

            var projectAnalysisResult = results.Result.ProjectAnalysisResults[0];
            var preportReferences = projectAnalysisResult.PreportMetaReferences;
            var metaReferences = projectAnalysisResult.MetaReferences;
            var externalReferences = projectAnalysisResult.ExternalReferences;
            var projectRules = projectAnalysisResult.ProjectRules;

            var fileResults = _portingAssistantClient.AnalyzeFileAsync(_sourceFileAnalysisResult.SourceFilePath, "", _tmpSolutionFileName,
                preportReferences, metaReferences, projectRules, externalReferences, new AnalyzerSettings { TargetFramework = "netcoreapp3.1" });
            fileResults.Wait();

            var fileSourceFileAnalysis = fileResults.Result;

            Assert.AreEqual(fileSourceFileAnalysis.Count, 1);
            Assert.AreEqual(fileSourceFileAnalysis[0], _sourceFileAnalysisResult);
        }

        [Test]
        public void AnalyzeFileSucceedsTestWithFileContents()
        {
            var results = _portingAssistantClient.AnalyzeSolutionAsync(Path.Combine(_solutionFolder, "SolutionWithProjects.sln"), new AnalyzerSettings { TargetFramework = "netcoreapp3.1", ContiniousEnabled = true, CompatibleOnly = true });
            results.Wait();

            var projectAnalysisResult = results.Result.ProjectAnalysisResults[0];
            var preportReferences = projectAnalysisResult.PreportMetaReferences;
            var metaReferences = projectAnalysisResult.MetaReferences;
            var externalReferences = projectAnalysisResult.ExternalReferences;
            var projectRules = projectAnalysisResult.ProjectRules;

            var fileResults = _portingAssistantClient.AnalyzeFileAsync(_sourceFileAnalysisResult.SourceFilePath, "", "", _tmpSolutionFileName,
                preportReferences, metaReferences, projectRules, externalReferences, new AnalyzerSettings { TargetFramework = "netcoreapp3.1" });
            fileResults.Wait();

            var fileSourceFileAnalysis = fileResults.Result;

            Assert.AreEqual(fileSourceFileAnalysis.Count, 1);
            Assert.AreEqual(fileSourceFileAnalysis[0], _sourceFileAnalysisResult);
        }

        [Test]
        public void DisposeProjectAnalysisResultSucceedsTest()
        {
            var results = _portingAssistantClient.AnalyzeSolutionAsync(Path.Combine(_solutionFolder, "SolutionWithProjects.sln"), new AnalyzerSettings { TargetFramework = "net6.0" });
            results.Wait();
            var projectAnalysisResult = results.Result.ProjectAnalysisResults.Find(p => p.ProjectName == "Nop.Core");

            projectAnalysisResult.Dispose();

            Assert.AreEqual(null, projectAnalysisResult.Errors);
            Assert.AreEqual(null, projectAnalysisResult.SourceFileAnalysisResults);
            Assert.AreEqual(null, projectAnalysisResult.PackageAnalysisResults);
            Assert.AreEqual(null, projectAnalysisResult.PreportMetaReferences);
            Assert.AreEqual(null, projectAnalysisResult.MetaReferences);
            Assert.AreEqual(null, projectAnalysisResult.ProjectRules);
            Assert.AreEqual(null, projectAnalysisResult.VisualBasicProjectRules);
            Assert.AreEqual(null, projectAnalysisResult.ExternalReferences);
            Assert.AreEqual(null, projectAnalysisResult.ProjectCompatibilityResult);
            Assert.AreEqual(0, projectAnalysisResult.LinesOfCode);
        }
    }
}