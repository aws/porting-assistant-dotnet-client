using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PortingAssistant.ApiAnalysis;
using PortingAssistant.Handler;
using PortingAssistant.Utils;
using PortingAssistant.NuGet;
using PortingAssistant.Model;
using PortingAssistant.Porting;
using PortingAssistant.ApiAnalysis.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Tests
{
    public class PortingAssistantHandlerTest
    {

        private Mock<IPortingAssistantNuGetHandler> _nuGetHandlerMock;
        private Mock<IPortingAssistantApiAnalysisHandler> _apiAnalysisHandlerMock;
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
            _nuGetHandlerMock = new Mock<IPortingAssistantNuGetHandler>();
            _apiAnalysisHandlerMock = new Mock<IPortingAssistantApiAnalysisHandler>();
            _portingHandlerMock = new Mock<IPortingHandler>();
            _portingAssistantHandler = new PortingAssistantHandler(
                NullLogger<PortingAssistantHandler>.Instance,
                _nuGetHandlerMock.Object,
                _apiAnalysisHandlerMock.Object,
                _portingHandlerMock.Object);
        }

        [SetUp]
        public void SetUp()
        {
            _nuGetHandlerMock.Reset();

            // Setup Nuget Dependencies
            _nuGetHandlerMock
                .Setup(nuGet => nuGet.GetNugetPackages(It.IsAny<List<PackageVersionPair>>(), It.IsAny<string>()))
                .Returns((List<PackageVersionPair> list, string pathToSolution) =>
                {
                    return list.Distinct().Select(packageVersion =>
                    {
                        var taskCompletionSource = new TaskCompletionSource<PackageDetails>();
                        taskCompletionSource.SetResult(
                            _packageDetails
                            );
                        return new Tuple<PackageVersionPair, Task<PackageDetails>>(
                            packageVersion, taskCompletionSource.Task
                        );
                    }).ToDictionary(t => t.Item1, t => t.Item2);
                });

            _apiAnalysisHandlerMock.Reset();
            _apiAnalysisHandlerMock.Setup(analyzer => analyzer.AnalyzeSolution(It.IsAny<string>(), It.IsAny<List<ProjectDetails>>()))
                .Returns((string solutionFilePath, List<ProjectDetails> projects) =>
                {
                    var taskCompletionSource = new TaskCompletionSource<ProjectApiAnalysisResult>();
                    taskCompletionSource.SetResult(new ProjectApiAnalysisResult {
                        SourceFileAnalysisResults = new List<SourceFileAnalysisResult>
                        {
                            _sourceFileAnalysisResult
                        }
                    });

                    return new SolutionApiAnalysisResult
                    {
                        ProjectApiAnalysisResults = projects.Select(p => {
                            return new Tuple<string, Task<ProjectApiAnalysisResult>>(p.ProjectFilePath, taskCompletionSource.Task);
                        }).ToDictionary(t => t.Item1, t => t.Item2)
                    };
                });
        }
        
        [Test]
        public void TestGetSolutionDetails()
        {
            var solutionDetail = _portingAssistantHandler.GetSolutionDetails(
                Path.Combine(_solutionFolder, "SolutionWithProjects.sln")
            );
            Assert.AreEqual("SolutionWithProjects", solutionDetail.SolutionName);
            Assert.AreEqual(1, solutionDetail.Projects.Find(p => p.ProjectName == "PortingAssistantApi").ProjectReferences.Count);
            Assert.AreEqual(4, solutionDetail.Projects.Find(p => p.ProjectName == "PortingAssistantApi").PackageReferences.Count);
            Assert.AreEqual(8, solutionDetail.Projects.Find(p => p.ProjectName == "Nop.Core").PackageReferences.Count);

            Assert.AreEqual(5, solutionDetail.Projects.Count);
            Assert.Contains("PortingAssistantApi", solutionDetail.Projects.Select(result => result.ProjectName).ToList());

            var project = solutionDetail.Projects.First(project => project.ProjectName.Equals("PortingAssistantApi"));
            Assert.Contains("nunit", project.PackageReferences.Select(dep => dep.PackageId).ToList());
            Assert.Contains(Path.Combine(_solutionFolder, "PortingAssistantAssessment", "PortingAssistantAssessment.csproj"),
                project.ProjectReferences.Select(proj => proj.ReferencePath).ToList());

            project = solutionDetail.Projects.First(project => project.ProjectName.Equals("Nop.Core"));
            Assert.Contains("Autofac", project.PackageReferences.Select(dep => dep.PackageId).ToList());
            Assert.Contains(".NETFramework 4.5.1", project.TargetFrameworks);
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
            var results = _portingAssistantHandler.AnalyzeSolution(Path.Combine(_solutionFolder, "SolutionWithProjects.sln"), new Settings());
            var projectAnalysisResult = results.ProjectAnalysisResult.Find(p => p.ProjectName == "Nop.Core");
            var projectApiAnalysisResult = projectAnalysisResult.ProjectApiAnalysisResult;
            var packageAnalysisResult = projectAnalysisResult.PackageAnalysisResults;

            projectApiAnalysisResult.Wait();
            Assert.AreEqual(_sourceFileAnalysisResult, projectApiAnalysisResult.Result.SourceFileAnalysisResults.First());

            Task.WaitAll(packageAnalysisResult.Values.ToArray());
            var packageResult = packageAnalysisResult.First(p => p.Value.Result.PackageVersionPair.PackageId == _packageDetails.Name);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageResult.Value.Result.Recommendations.RecommendedActions.First().RecommendedActionType); ;
            var compatibilityResult = packageResult.Value.Result.CompatibilityResults.GetValueOrDefault(PackageCompatibility.DEFAULT_TARGET);
            Assert.AreEqual(Compatibility.COMPATIBLE, compatibilityResult.Compatibility);
            Assert.AreEqual("12.0.4", compatibilityResult.CompatibleVersions.First());
        }
        
        [Test]
        public void GetProjectWithCorruptedSolutionFileThrowsException()
        {
            // TODO: this unit test fails
            var testSolutionPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithFailedContent", "NopCommerce.sln");

            Assert.Throws<PortingAssistantException>(() =>
            {
                _portingAssistantHandler.GetSolutionDetails(Path.Combine(_solutionFolder, testSolutionPath));
            });
        }
    }
}