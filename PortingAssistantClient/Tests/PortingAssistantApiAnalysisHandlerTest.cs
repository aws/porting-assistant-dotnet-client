using System;
using System.Collections.Generic;
using PortingAssistant.Model;
using System.Linq;
using PortingAssistant.NuGet;
using PortingAssistant.ApiAnalysis.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Build.Construction;
using PortingAssistant.Handler.FileParser;
using NuGet.Frameworks;
using NuGet.Versioning;
using PortingAssistant.ApiAnalysis;

namespace Tests
{
    public class PortingAssistantApiAnalysisHandlerTest
    {
        private Mock<IPortingAssistantNuGetHandler> _nuGetHandlerMock;
        private Mock<IPortingAssistantRecommendationHandler> _recommendationHandlerMock;
        private PortingAssistantApiAnalysisHandler _apiAnalysisHandler;
        private string _solutionFile;
        private List<ProjectDetails> _projects;

        private readonly PackageDetails _packageDetails = new PackageDetails
        {
            Name = "Newtonsoft.Json",
            Versions = new SortedSet<string> { "12.0.3", "12.0.4" },
            ApiDetails = new ApiDetails[]
            {
                new ApiDetails
                {
                    MethodName = "Setup(Object)",
                    MethodSignature = "Newtonsoft.Json.JsonConvert.SerializeObject(object)",
                    Targets = new Dictionary<string, SortedSet<string>>
                    {
                        {
                             "netcoreapp3.1", new SortedSet<string> { "10.2.0","12.0.3", "12.0.4" }
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

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _nuGetHandlerMock = new Mock<IPortingAssistantNuGetHandler>();
            _recommendationHandlerMock = new Mock<IPortingAssistantRecommendationHandler>();
            _apiAnalysisHandler = new PortingAssistantApiAnalysisHandler(NullLogger<PortingAssistantApiAnalysisHandler>.Instance, _nuGetHandlerMock.Object, _recommendationHandlerMock.Object);
        }

        [SetUp]
        public void SetUp()
        {
            _solutionFile = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithApi", "SolutionWithApi.sln");
            _projects = GetProjects(_solutionFile);

            _nuGetHandlerMock.Reset();
            _nuGetHandlerMock.Setup(handler => handler.GetPackageDetails(It.IsAny<PackageVersionPair>()))
                .Returns((PackageVersionPair package) =>
                {
                    var task = new TaskCompletionSource<PackageDetails>();
                    task.SetResult(_packageDetails);
                    return task.Task;
                });

            _nuGetHandlerMock.Setup(handler => handler.GetNugetPackages(It.IsAny<List<PackageVersionPair>>(), It.IsAny<string>()))
                .Returns((List<PackageVersionPair> packageVersionPairs, string path) =>
                {
                    var task = new TaskCompletionSource<PackageDetails>();
                    task.SetResult(new PackageDetails
                    {
                        Namespaces = new string[]{ "TestNamespace"},
                    });
                    return new Dictionary<PackageVersionPair, Task<PackageDetails>>
                    {
                        {new PackageVersionPair(), task.Task }
                    };
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
                    ProjectType = p.ProjectType.ToString(),
                    TargetFrameworks = projectParser.GetTargetFrameworks().Select(tfm =>
                    {
                        var framework = NuGetFramework.Parse(tfm);
                        return string.Format("{0} {1}", framework.Framework, NuGetVersion.Parse(framework.Version.ToString()).ToNormalizedString());
                    }).ToList(),
                    PackageReferences = projectParser.GetPackageReferences(),
                    ProjectReferences = projectParser.GetProjectReferences()
                };
            }).Where(p => p != null).ToList();

            return projects;
        }

        [Test]
        public void AnalyzeWellDefinedSolutionSucceeds()
        {
            var result = _apiAnalysisHandler.AnalyzeSolution(_solutionFile, _projects);
            Task.WaitAll(result.ProjectApiAnalysisResults.Values.ToArray());

            var values = result.ProjectApiAnalysisResults.Values.First().Result;

            Assert.AreEqual("Newtonsoft.Json", values.SourceFileAnalysisResults.First().ApiAnalysisResults.First().CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("11.0.1", values.SourceFileAnalysisResults.First().ApiAnalysisResults.First().CodeEntityDetails.Package.Version);
            Assert.AreEqual("Newtonsoft.Json.JsonConvert.SerializeObject(object)",
                values.SourceFileAnalysisResults.First().ApiAnalysisResults.First().CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual(Compatibility.COMPATIBLE, values.SourceFileAnalysisResults.First().ApiAnalysisResults.First().CompatibilityResults.GetValueOrDefault(ApiCompatiblity.DEFAULT_TARGET));
            Assert.AreEqual("12.0.4", values.SourceFileAnalysisResults.First().ApiAnalysisResults.First().Recommendations.RecommendedActions.First().Description);
        }

        [Test]
        public void AnalyzeNullPathThrowsException()
        {
            Assert.Throws<AggregateException>(() =>
            {
                var result = _apiAnalysisHandler.AnalyzeSolution(Path.Combine(_solutionFile, "Rand.sln"), _projects);
                Task.WaitAll(result.ProjectApiAnalysisResults.Values.ToArray());
            });
        }

        [Test]
        public void AnalyzeNonexistentSolutionThrowsException()
        {
            Assert.Throws<AggregateException>(() =>
            {
                var result = _apiAnalysisHandler.AnalyzeSolution(Path.Combine(_solutionFile, "Rand.sln"), _projects);
                Task.WaitAll(result.ProjectApiAnalysisResults.Values.ToArray());
            });
        }
    }
}
