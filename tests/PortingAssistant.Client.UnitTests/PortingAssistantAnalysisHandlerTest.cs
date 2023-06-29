using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PortingAssistant.Client.Analysis;
using PortingAssistant.Client.Common.Model;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.NuGet;

namespace PortingAssistant.Client.Tests
{

    public class PortingAssistantAnalysisHandlerTest
    {
        private Mock<ILogger<PortingAssistantAnalysisHandler>> _loggerMock;
        private Mock<IPortingAssistantNuGetHandler> _nuGetHandlerMock;
        private Mock<IPortingAssistantRecommendationHandler> _recommendationHandlerMock;
        private PortingAssistantAnalysisHandler _analysisHandler;
        private string _solutionFile;
        private List<ProjectDetails> _projects;
        private List<string> _projectPaths;
        private string _vbSolutionFile;
        private List<ProjectDetails> _vbProjects;
        private List<string> _vbProjectPaths;
        private string DEFAULT_TARGET = "net6.0";

        private readonly PackageDetails _packageDetails = new PackageDetails
        {
            Name = "Newtonsoft.Json",
            Versions = new SortedSet<string> { "12.0.3", "12.0.4", "13.0.2" },
            Api = new ApiDetails[]
            {
                new ApiDetails
                {
                    MethodName = "Setup(Object)",
                    MethodSignature = "Newtonsoft.Json.JsonConvert.SerializeObject(object)",
                    Targets = new Dictionary<string, SortedSet<string>>
                    {
                        {
                             "netcoreapp3.1", new SortedSet<string> { "10.2.0", "12.0.3", "12.0.4", "13.0.2" }
                        },
                        {
                             "net6.0", new SortedSet<string> { "10.2.0", "12.0.3", "12.0.4", "13.0.2" }
                        }
                    },
                },
                new ApiDetails
                {
                    MethodName = "Setup(Object)",
                    MethodSignature = "Newtonsoft.Json.JsonConvert.SerializeObject(object?)",
                    Targets = new Dictionary<string, SortedSet<string>>
                    {
                        {
                            "netcoreapp3.1", new SortedSet<string> { "10.2.0", "12.0.3", "12.0.4", "13.0.2" }
                        },
                        {
                            "net6.0", new SortedSet<string> { "10.2.0", "12.0.3", "12.0.4", "13.0.2" }
                        }
                    },
                },
                new ApiDetails
                {
                    MethodName = "SerializeObject",
                    MethodSignature = "Public Shared Overloads Function SerializeObject(value As Object) As String",
                    Targets = new Dictionary<string, SortedSet<string>>
                    {
                        {
                             "netcoreapp3.1", new SortedSet<string> { "10.2.0", "12.0.3", "12.0.4", "13.0.2" }
                        },
                        {
                             "net6.0", new SortedSet<string> { "10.2.0", "12.0.3", "12.0.4", "13.0.2" }
                        }
                    },
                }
            },
            Targets = new Dictionary<string, SortedSet<string>> {
                {
                    "netcoreapp3.1",
                    new SortedSet<string> { "12.0.3", "12.0.4", "13.0.2" }
                },
                {
                    "net6.0",
                    new SortedSet<string> { "12.0.3", "12.0.4", "13.0.2" }
                }
            },
            License = new LicenseDetails
            {
                License = new Dictionary<string, SortedSet<string>>
                {
                    { "MIT", new SortedSet<string> { "12.0.3", "12.0.4", "13.0.2" } }
                }
            },
            Namespaces = new string[] { "TestNamespace" }
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _nuGetHandlerMock = new Mock<IPortingAssistantNuGetHandler>();
            _recommendationHandlerMock = new Mock<IPortingAssistantRecommendationHandler>();
            _analysisHandler = new PortingAssistantAnalysisHandler(TestLogger.Create<PortingAssistantAnalysisHandler>(), _nuGetHandlerMock.Object, _recommendationHandlerMock.Object);
        }

        [SetUp]
        public void SetUp()
        {
            _solutionFile = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithApi", "SolutionWithApi.sln");
            _projects = GetProjects(_solutionFile);
            _projectPaths = _projects.ConvertAll(p => p.ProjectFilePath);

            _vbSolutionFile = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "VBSolutionWithApi", "VBSolutionWithApi.sln");
            _vbProjects = GetProjects(_vbSolutionFile);
            _vbProjectPaths = _vbProjects.ConvertAll(p => p.ProjectFilePath);

            _nuGetHandlerMock.Reset();

            _nuGetHandlerMock.Setup(handler => handler.GetNugetPackages(It.IsAny<List<PackageVersionPair>>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns((List<PackageVersionPair> packageVersionPairs, string path, bool isIncremental, bool incrementalRefresh) =>
                {
                    var task = new TaskCompletionSource<PackageDetails>();
                    task.SetResult(_packageDetails);
                    return new Dictionary<PackageVersionPair, Task<PackageDetails>>
                    {
                        {
                            new PackageVersionPair
                            {
                                PackageId = "Newtonsoft.Json",
                                Version = "13.0.1",
                                PackageSourceType = PackageSourceType.NUGET
                            },
                            task.Task 
                        }
                    };
                });
            _recommendationHandlerMock.Reset();
            _recommendationHandlerMock.Setup(handler => handler.GetApiRecommendation(It.IsAny<List<string>>()))
                .Returns((List<string> packageVersionPairs) =>
                {
                    return new Dictionary<string, Task<RecommendationDetails>>();
                });

        }

        private IPortingAssistantAnalysisHandler GetPortingAssistantAnalysisHandlerWithException()
        {
            _loggerMock = new Mock<ILogger<PortingAssistantAnalysisHandler>>();

            _loggerMock.Reset();

            _loggerMock.Setup(_ => _.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsValueType>(),
                It.IsAny<Exception>(),
                (Func<It.IsValueType, Exception, string>)It.IsAny<object>()));

            return new PortingAssistantAnalysisHandler(_loggerMock.Object, _nuGetHandlerMock.Object, _recommendationHandlerMock.Object);
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

                return new ProjectDetails
                {
                    ProjectName = p.ProjectName,
                    ProjectFilePath = p.AbsolutePath,
                    ProjectGuid = p.ProjectGuid,
                    ProjectType = p.ProjectType.ToString(),
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
                Version = "13.0.1",
                PackageSourceType = PackageSourceType.NUGET
            };
            var result = _analysisHandler.AnalyzeSolution(_solutionFile, _projectPaths);
            Task.WaitAll(result);

            var projectAnalysisResult = result.Result.Values.First();
            Task.WaitAll(projectAnalysisResult.PackageAnalysisResults.Values.ToArray());
            var packageAnalysisResult = projectAnalysisResult.PackageAnalysisResults.First(p => p.Key.PackageId == "Newtonsoft.Json").Value.Result;

            Assert.AreEqual(package, packageAnalysisResult.PackageVersionPair);
            Assert.AreEqual(Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
            Assert.AreEqual("13.0.2", packageAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).CompatibleVersions.First());
            Assert.AreEqual("13.0.2", packageAnalysisResult.Recommendations.RecommendedActions.First().Description);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);

            var sourceFile = projectAnalysisResult.SourceFileAnalysisResults.Find(s => s.SourceFileName == "Program.cs");
            Assert.NotNull(sourceFile);
            Assert.NotNull(sourceFile.ApiAnalysisResults);

            var apiAnalysisResult = sourceFile.ApiAnalysisResults.Find(r => r.CodeEntityDetails.OriginalDefinition == "Newtonsoft.Json.JsonConvert.SerializeObject(object?)");
            Assert.NotNull(apiAnalysisResult);

            Assert.AreEqual("Newtonsoft.Json", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("13.0.1", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual("Newtonsoft.Json.JsonConvert.SerializeObject(object?)",
                apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual(Compatibility.COMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
            Assert.AreEqual("13.0.2", apiAnalysisResult.Recommendations.RecommendedActions.First().Description);
        }

        [Test]
        public void VBAnalyzeWellDefinedSolutionSucceeds()
        {
            var package = new PackageVersionPair
            {
                PackageId = "Newtonsoft.Json",
                Version = "13.0.1",
                PackageSourceType = PackageSourceType.NUGET
            };
            var result = _analysisHandler.AnalyzeSolution(_vbSolutionFile, _vbProjectPaths);
            Task.WaitAll(result);

            var projectAnalysisResult = result.Result.Values.First();
            Task.WaitAll(projectAnalysisResult.PackageAnalysisResults.Values.ToArray());
            var packageAnalysisResult = projectAnalysisResult.PackageAnalysisResults.First(p => p.Key.PackageId == "Newtonsoft.Json").Value.Result;

            Assert.AreEqual(package, packageAnalysisResult.PackageVersionPair);
            Assert.AreEqual(Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
            Assert.AreEqual("13.0.2", packageAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).CompatibleVersions.First());
            Assert.AreEqual("13.0.2", packageAnalysisResult.Recommendations.RecommendedActions.First().Description);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageAnalysisResult.Recommendations.RecommendedActions.First().RecommendedActionType);

            var sourceFile = projectAnalysisResult.SourceFileAnalysisResults.Find(s => s.SourceFileName == "Program.vb");
            Assert.NotNull(sourceFile);
            Assert.NotNull(sourceFile.ApiAnalysisResults);

            var apiAnalysisResult = sourceFile.ApiAnalysisResults.Find(r => r.CodeEntityDetails.OriginalDefinition == "Newtonsoft.Json.JsonConvert.SerializeObject(Object)");
            Assert.NotNull(apiAnalysisResult);

            Assert.AreEqual("Newtonsoft.Json", apiAnalysisResult.CodeEntityDetails.Package.PackageId);
            Assert.AreEqual("13.0.1", apiAnalysisResult.CodeEntityDetails.Package.Version);
            Assert.AreEqual("Newtonsoft.Json.JsonConvert.SerializeObject(Object)",
                apiAnalysisResult.CodeEntityDetails.OriginalDefinition);
            Assert.AreEqual(Compatibility.COMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
            Assert.AreEqual("13.0.2", apiAnalysisResult.Recommendations.RecommendedActions.First().Description);
        }

        [Test]
        public void AnalyzeBadProjectDoesNotThrowException()
        {
            var result = _analysisHandler.AnalyzeSolution(_solutionFile, new List<string> { "Rand.csproj" });
            Task.WaitAll(result);
            Assert.IsNull(result.Result.GetValueOrDefault("Rand.csproj", null));
        }

        [Test]
        public void AnalyzeNullPathThrowsException()
        {
            Assert.Throws<AggregateException>(() =>
            {
                var result = _analysisHandler.AnalyzeSolution(null, _projectPaths);
                Task.WaitAll(result);
            });
        }

        [Test]
        public void AnalyzeNonexistentSolutionThrowsException()
        {
            Assert.Throws<AggregateException>(() =>
            {
                var result = _analysisHandler.AnalyzeSolution(Path.Combine(_solutionFile, "Rand.sln"), _projectPaths);
                Task.WaitAll(result);
            });
        }

        [Test]
        public void AnalyzeFileSucceeds()
        {
            var filePath = Path.Combine(_solutionFile.Replace("\\SolutionWithApi.sln",""), "testproject", "Program.cs");
            var projectPath = Path.Combine(_solutionFile.Replace("\\SolutionWithApi.sln", ""), "testproject", "testproject.csproj");

            var result = _analysisHandler.AnalyzeSolutionIncremental(_solutionFile, _projectPaths);
            
            Task.WaitAll(result);

            var projectAnalysisResult = result.Result[projectPath];
            var preportReferences = projectAnalysisResult.PreportMetaReferences;
            var metaReferences = projectAnalysisResult.MetaReferences;
            var externalReferences = projectAnalysisResult.ExternalReferences;
            var projectRules = projectAnalysisResult.ProjectRules;

            var incrementalResult = _analysisHandler.AnalyzeFileIncremental(filePath, projectPath, _solutionFile, preportReferences, metaReferences,
                projectRules, externalReferences, false, true, "netcoreapp3.1");
            Task.WaitAll(incrementalResult);

            var fileAnalysisResult = incrementalResult.Result;

            var sourceFile = fileAnalysisResult.Find(s => s.SourceFileName == "Program.cs");
            Assert.NotNull(sourceFile);
            Assert.IsEmpty(sourceFile.ApiAnalysisResults);
        }

        [Test]
        public void AnalyzeProjectFilesThrowsException()
        {
            var analysisHandlerWithException = GetPortingAssistantAnalysisHandlerWithException();

            var filePath = Path.Combine(_solutionFile.Replace("\\SolutionWithApi.sln", ""), "testproject", "ProgramIncorrect.cs");
            var projectPath = Path.Combine(_solutionFile.Replace("\\SolutionWithApi.sln", ""), "testproject", "testproject.csproj");

            Assert.Throws <System.AggregateException> (() =>
            {
                var incrementalResult = analysisHandlerWithException.AnalyzeFileIncremental(filePath, "", "IncorrectPath", _solutionFile, new List<string>(), new List<string>(),
                null, null, false, true, "netcoreapp3.1");
                Task.WaitAll(incrementalResult);
                _loggerMock.Verify(x => x.LogError(It.IsAny<Exception>(), "Error while analyzing files"), Times.Once);
            });
        }

        [Test]
        public void AnalyzeFileIncrementalReturnsEmptyCollection() 
        {
            // empty collection returned when projectPath isn't valid
            var incrementalResult = _analysisHandler.AnalyzeFileIncremental("IncorrectPath", "", "IncorrectPath", _solutionFile, new List<string>(), new List<string>(),
                null, null, false, true, "netcoreapp3.1");
            Task.WaitAll(incrementalResult);
            Assert.IsEmpty(incrementalResult.Result);
        }

        [Test]
        public void ProjectCompatibilityResultToStringTest()
        {
            var package = new PackageVersionPair
            {
                PackageId = "Newtonsoft.Json",
                Version = "13.0.1",
                PackageSourceType = PackageSourceType.NUGET
            };
            var result = _analysisHandler.AnalyzeSolution(_solutionFile, _projectPaths);
            Task.WaitAll(result);

            var projectAnalysisResult = result.Result.Values.First();
            Task.WaitAll(projectAnalysisResult.PackageAnalysisResults.Values.ToArray());
            var projectCompatibilityResult = projectAnalysisResult.ProjectCompatibilityResult;

            var str = projectCompatibilityResult.ToString();

            Assert.AreEqual("Analyzed Project Compatibilities for " + _projectPaths[0] + ":" + Environment.NewLine +
                            "Annotation: Compatible:0, Incompatible:0, Unknown:2, Deprecated:0, Actions:0" + Environment.NewLine +
                            "Method: Compatible:1, Incompatible:0, Unknown:1, Deprecated:0, Actions:0" + Environment.NewLine +
                            "Declaration: Compatible:1, Incompatible:0, Unknown:17, Deprecated:0, Actions:0" + Environment.NewLine +
                            "Enum: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0" + Environment.NewLine +
                            "Struct: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0" + Environment.NewLine, str);
        }
    }
}
