using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using PortingAssistant.Client.Analysis;
using PortingAssistant.Client.Analysis.Utils;
using PortingAssistant.Client.Handler;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.Porting;

namespace PortingAssistant.Client.Tests
{
    public class PortingAssistantHandlerTest
    {
        private Mock<IPortingAssistantAnalysisHandler> _apiAnalysisHandlerMock;
        private Mock<IPortingHandler> _portingHandlerMock;
        private PortingAssistantHandler _portingAssistantHandler;
        private readonly string _solutionFolder = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithProjects");

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
                        }
                    },
                }
            },
            Targets = new Dictionary<string, SortedSet<string>> {
                {
                    "netcoreapp3.1",
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
                        { ApiCompatiblity.DEFAULT_TARGET, new CompatibilityResult{
                            Compatibility = Compatibility.COMPATIBLE,
                            CompatibleVersions = new List<string>{ "12.0.3", "12.0.4" }
                        } }
                    }
                }
            }
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _apiAnalysisHandlerMock = new Mock<IPortingAssistantAnalysisHandler>();
            _portingHandlerMock = new Mock<IPortingHandler>();
            _portingAssistantHandler = new PortingAssistantHandler(
                NullLogger<PortingAssistantHandler>.Instance,
                _apiAnalysisHandlerMock.Object,
                _portingHandlerMock.Object);
        }

        [SetUp]
        public void SetUp()
        {

            _apiAnalysisHandlerMock.Reset();
            _apiAnalysisHandlerMock.Setup(analyzer => analyzer.AnalyzeSolution(It.IsAny<string>(), It.IsAny<List<ProjectDetails>>()))
                .Returns((string solutionFilePath, List<ProjectDetails> projects) =>
                {
                    return projects.Select(project =>
                    {
                        var package = new PackageVersionPair
                        {
                            PackageId = "Newtonsoft.Json",
                            Version = "11.0.1"
                        };
                        var taskCompletionSource = new TaskCompletionSource<ProjectAnalysisResult>();
                        var taskPackageCompletionSource = new TaskCompletionSource<PackageAnalysisResult>();
                        taskPackageCompletionSource.SetResult(new PackageAnalysisResult
                        {
                            PackageVersionPair = package,
                            CompatibilityResults = new Dictionary<string, CompatibilityResult>
                            {
                                {"netcoreapp3.1", new CompatibilityResult{
                                    Compatibility = Compatibility.COMPATIBLE,
                                    CompatibleVersions = new List<string>
                                    {
                                        "12.0.3", "12.0.4"
                                    }
                                }}
                            },
                            Recommendations = new Recommendations
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

                        taskCompletionSource.SetResult(new ProjectAnalysisResult
                        {
                            ProjectName = project.ProjectName,
                            ProjectFile = project.ProjectFilePath,
                            PackageAnalysisResults = new Dictionary<PackageVersionPair, Task<PackageAnalysisResult>>
                            {
                                {package, taskPackageCompletionSource.Task }
                            },
                            SourceFileAnalysisResults = new List<SourceFileAnalysisResult>
                            {
                                _sourceFileAnalysisResult
                            }
                        });

                        return new Tuple<string, Task<ProjectAnalysisResult>>(project.ProjectFilePath, taskCompletionSource.Task);
                    }).ToDictionary(t => t.Item1, t => t.Item2);
                });
        }

        [Test]
        public void TestGetSolutionDetails()
        {
            var solutionDetail = _portingAssistantHandler.GetSolutionDetails(
                Path.Combine(_solutionFolder, "SolutionWithProjects.sln")
            );
            Assert.AreEqual("SolutionWithProjects", solutionDetail.SolutionName);

            Assert.AreEqual(5, solutionDetail.Projects.Count);
            Assert.Contains("PortingAssistantApi", solutionDetail.Projects.Select(result => result.ProjectName).ToList());

            var project = solutionDetail.Projects.First(project => project.ProjectName.Equals("PortingAssistantApi"));
        }


        [Test]
        public void GetSolutionDetailsForNonexistentSolutionThrowsException()
        {
            Assert.Throws<PortingAssistantException>(() =>
            {
                _portingAssistantHandler.GetSolutionDetails(Path.Combine(_solutionFolder, "NonexistentSolution.sln"));
            });
        }

        [Test]
        public void AnalyzeSolutionWithProjectsSucceeds()
        {
            var results = _portingAssistantHandler.AnalyzeSolutionAsync(Path.Combine(_solutionFolder, "SolutionWithProjects.sln"), new Settings());
            Task.WaitAll(results);
            var projectAnalysisResult = results.Result.ProjectAnalysisResults.Find(p => p.ProjectName == "Nop.Core");
            var sourceFileAnalysisResults = projectAnalysisResult.SourceFileAnalysisResults;
            var packageAnalysisResult = projectAnalysisResult.PackageAnalysisResults;

            Assert.AreEqual(_sourceFileAnalysisResult, sourceFileAnalysisResults.First());

            Task.WaitAll(packageAnalysisResult.Values.ToArray());
            var packageResult = packageAnalysisResult.First(p => p.Value.Result.PackageVersionPair.PackageId == _packageDetails.Name);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageResult.Value.Result.Recommendations.RecommendedActions.First().RecommendedActionType); ;
            var compatibilityResult = packageResult.Value.Result.CompatibilityResults.GetValueOrDefault(PackageCompatibility.DEFAULT_TARGET);
            Assert.AreEqual(Compatibility.COMPATIBLE, compatibilityResult.Compatibility);
            Assert.AreEqual("12.0.3", compatibilityResult.CompatibleVersions.First());
        }

        [Test]
        public void GetProjectWithCorruptedSolutionFileThrowsException()
        {
            var testSolutionPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithFailedContent", "NopCommerce.sln");

            Assert.Throws<PortingAssistantException>(() =>
            {
                _portingAssistantHandler.GetSolutionDetails(Path.Combine(_solutionFolder, testSolutionPath));
            });
        }
    }
}