using System;
using System.Collections.Generic;
using PortingAssistant.Model;
using System.Linq;
using PortingAssistant.NuGet;
using PortingAssistant.Analysis.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Build.Construction;
using PortingAssistant.Handler.FileParser;
using NuGet.Frameworks;
using NuGet.Versioning;
using PortingAssistant.Analysis;

namespace Tests
{

    public class PortingAssistantApiAnalysisHandlerTest
    {
        private Mock<IPortingAssistantNuGetHandler> _nuGetHandlerMock;
        private Mock<IPortingAssistantRecommendationHandler> _recommendationHandlerMock;
        private PortingAssistantAnalysisHandler _analysisHandler;
        private string _solutionFile;
        private List<ProjectDetails> _projects;

        private readonly PackageDetails _packageDetails = new PackageDetails
        {
            Name = "Newtonsoft.Json",
            Versions = new SortedSet<string> { "12.0.3", "12.0.4" },
            Api = new ApiDetails[]
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
            },
            Namespaces = new string[] { "TestNamespace" }
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _nuGetHandlerMock = new Mock<IPortingAssistantNuGetHandler>();
            _recommendationHandlerMock = new Mock<IPortingAssistantRecommendationHandler>();
            _analysisHandler = new PortingAssistantAnalysisHandler(NullLogger<PortingAssistantAnalysisHandler>.Instance, _nuGetHandlerMock.Object, _recommendationHandlerMock.Object);
        }

        [SetUp]
        public void SetUp()
        {
            _solutionFile = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithApi", "SolutionWithApi.sln");
            _projects = GetProjects(_solutionFile);

            _nuGetHandlerMock.Reset();

            _nuGetHandlerMock.Setup(handler => handler.GetNugetPackages(It.IsAny<List<PackageVersionPair>>(), It.IsAny<string>()))
                .Returns((List<PackageVersionPair> packageVersionPairs, string path) =>
                {
                    var task = new TaskCompletionSource<PackageDetails>();
                    task.SetResult(_packageDetails);
                    return new Dictionary<PackageVersionPair, Task<PackageDetails>>
                    {
                        {new PackageVersionPair{
                            PackageId = "Newtonsoft.Json",
                            Version = "11.0.1"
                        }, task.Task }
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
            var package = new PackageVersionPair
            {
                PackageId = "Newtonsoft.Json",
                Version = "11.0.1"
            };
            var result = _analysisHandler.AnalyzeSolution(_solutionFile, _projects);
            Task.WaitAll(result.Values.ToArray());

            var projectAnalysisResult = result.Values.First().Result;
            Task.WaitAll(projectAnalysisResult.PackageAnalysisResults.Values.ToArray());
            var packageAnalysisResult = projectAnalysisResult.PackageAnalysisResults.First().Value.Result;

            Assert.AreEqual(package, packageAnalysisResult.PackageVersionPair);
            Assert.AreEqual(Compatibility.INCOMPATIBLE, packageAnalysisResult.CompatibilityResults.GetValueOrDefault(PackageCompatibility.DEFAULT_TARGET).Compatibility);
            Assert.AreEqual("12.0.3", packageAnalysisResult.CompatibilityResults.GetValueOrDefault(PackageCompatibility.DEFAULT_TARGET).CompatibleVersions.First());
            Assert.AreEqual("12.0.3", packageAnalysisResult.Recommendations.RecommendedActions.First().Description);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);

            Assert.AreEqual("Newtonsoft.Json", projectAnalysisResult.SourceFileAnalysisResults.First().ApiAnalysisResults.First().CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("11.0.1", projectAnalysisResult.SourceFileAnalysisResults.First().ApiAnalysisResults.First().CodeEntityDetails.Package.Version);
            Assert.AreEqual("Newtonsoft.Json.JsonConvert.SerializeObject(object)",
                projectAnalysisResult.SourceFileAnalysisResults.First().ApiAnalysisResults.First().CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual(Compatibility.COMPATIBLE, projectAnalysisResult.SourceFileAnalysisResults.First().ApiAnalysisResults.First().CompatibilityResults.GetValueOrDefault(ApiCompatiblity.DEFAULT_TARGET).Compatibility);
            Assert.AreEqual("12.0.3", projectAnalysisResult.SourceFileAnalysisResults.First().ApiAnalysisResults.First().Recommendations.RecommendedActions.First().Description);
        }

        [Test]
        public void AnalyzeNullPathThrowsException()
        {
            Assert.Throws<AggregateException>(() =>
            {
                var result = _analysisHandler.AnalyzeSolution(Path.Combine(_solutionFile, "Rand.sln"), _projects);
                Task.WaitAll(result.Values.ToArray());
            });
        }

        [Test]
        public void AnalyzeNonexistentSolutionThrowsException()
        {
            Assert.Throws<AggregateException>(() =>
            {
                var result = _analysisHandler.AnalyzeSolution(Path.Combine(_solutionFile, "Rand.sln"), _projects);
                Task.WaitAll(result.Values.ToArray());
            });
        }
    }
}
