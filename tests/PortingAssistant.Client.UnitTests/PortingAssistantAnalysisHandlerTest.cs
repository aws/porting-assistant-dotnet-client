using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Buildalyzer;
using LibGit2Sharp;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PortingAssistant.Client.Analysis;
using PortingAssistant.Client.Model;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using PortingAssistant.Compatibility.Common.Utils;
using PortingAssistant.Compatibility.Core;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using PackageSourceType = PortingAssistant.Client.Model.PackageSourceType;
using PackageVersionPair = PortingAssistant.Client.Model.PackageVersionPair;
using RecommendedActionType = PortingAssistant.Client.Model.RecommendedActionType;

namespace PortingAssistant.Client.Tests
{

    public class PortingAssistantAnalysisHandlerTest
    {
        private Mock<ILogger<PortingAssistantAnalysisHandler>> _loggerMock;
        private Mock<ICompatibilityChecker> _compatibilityCheckerMock;
        private Mock<ICompatibilityCheckerNuGetHandler> _nuGetHandlerMock;
        private Mock<ICompatibilityCheckerRecommendationHandler> _recommendationHandlerMock;
        private Mock<ICompatibilityCheckerHandler> _compatibilityCheckerHandlerMock;
        private Mock<ICacheService> _cacheServiceMock;
        private Mock<IHttpService> _httpService;
         
        private PortingAssistantAnalysisHandler _analysisHandler;
        private string _solutionFile;
        private List<ProjectDetails> _projects;
        private List<string> _projectPaths;
        private string _vbSolutionFile;
        private List<ProjectDetails> _vbProjects;
        private List<string> _vbProjectPaths;
        private string DEFAULT_TARGET = "net6.0";


        private readonly Compatibility.Common.Model.PackageDetails _packageDetails =
            new Compatibility.Common.Model.PackageDetails
        {
            Name = "Newtonsoft.Json",
            Versions = new SortedSet<string> { "12.0.3", "12.0.4", "13.0.2" },
            Api = new Compatibility.Common.Model.ApiDetails[]
            {
                new Compatibility.Common.Model.ApiDetails
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
                new Compatibility.Common.Model.ApiDetails
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
                new Compatibility.Common.Model.ApiDetails
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
            License = new Compatibility.Common.Model.LicenseDetails
            {
                License = new Dictionary<string, SortedSet<string>>
                {
                    { "MIT", new SortedSet<string> { "12.0.3", "12.0.4", "13.0.2" } }
                }
            },
            Namespaces = new string[] { "TestNamespace" }
        };

        private readonly Compatibility.Common.Model.PackageDetails _microsoft_sourcelink_GitHubpackageDetails =
            new Compatibility.Common.Model.PackageDetails
            {
                Name = "Microsoft.SourceLink.GitHub",
                Versions = new SortedSet<string> { "1.0.0", "2.0.0", "3.0.0" },
                
                Targets = new Dictionary<string, SortedSet<string>> {
                {
                    "netcoreapp3.1",
                    new SortedSet<string> { "1.0.0", "2.0.0", "3.0.0" }
                },
                {
                    "net6.0",
                    new SortedSet<string> { "1.0.0", "2.0.0", "3.0.0" }
                }
            },
                License = new Compatibility.Common.Model.LicenseDetails
                {
                    License = new Dictionary<string, SortedSet<string>>
                {
                    { "MIT", new SortedSet<string> { "1.0.0", "2.0.0", "3.0.0" } }
                }
                },
                Namespaces = new string[] { "TestNamespace" }
            };

        private readonly Compatibility.Common.Model.PackageDetails _nerdbank_gitVersioning_packageDetails =
            new Compatibility.Common.Model.PackageDetails
            {
                Name = "Nerdbank.GitVersioning",
                Versions = new SortedSet<string> { "3.4.231", "3.5.0", "3.6.0" },

                Targets = new Dictionary<string, SortedSet<string>> {
                {
                    "netcoreapp3.1",
                    new SortedSet<string> { "3.4.231", "3.5.0", "3.6.0" }
                },
                {
                    "net6.0",
                    new SortedSet<string> { "3.4.231", "3.5.0", "3.6.0" }
                }
            },
                License = new Compatibility.Common.Model.LicenseDetails
                {
                    License = new Dictionary<string, SortedSet<string>>
                {
                    { "MIT", new SortedSet<string> { "3.4.231", "3.5.0", "3.6.0" } }
                }
                },
                Namespaces = new string[] { "TestNamespace" }
            };
        

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _nuGetHandlerMock = new Mock<ICompatibilityCheckerNuGetHandler>();
            _compatibilityCheckerHandlerMock = new Mock<ICompatibilityCheckerHandler>(); 
            _compatibilityCheckerMock = new Mock<ICompatibilityChecker>();
            _recommendationHandlerMock = new Mock<ICompatibilityCheckerRecommendationHandler>();
            _cacheServiceMock = new Mock<ICacheService>();
            _httpService = new Mock<IHttpService>();
            _analysisHandler = new PortingAssistantAnalysisHandler(
                TestLogger.Create<PortingAssistantAnalysisHandler>(),
                new CompatibilityCheckerHandler(_nuGetHandlerMock.Object,
                _recommendationHandlerMock.Object, _httpService.Object, TestLogger.Create<CompatibilityCheckerHandler>()),
                _cacheServiceMock.Object);
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

            var task = new TaskCompletionSource<Compatibility.Common.Model.PackageDetails>();
            task.SetResult(_packageDetails);

            var microsoft_sourcelink_github_task = new TaskCompletionSource<Compatibility.Common.Model.PackageDetails>();
            microsoft_sourcelink_github_task.SetResult(_microsoft_sourcelink_GitHubpackageDetails);

            var nerdbank_gitVersioning_task = new TaskCompletionSource<Compatibility.Common.Model.PackageDetails>();
            nerdbank_gitVersioning_task.SetResult(_nerdbank_gitVersioning_packageDetails);
            

            var getNugetPackages = new Dictionary<Compatibility.Common.Model.PackageVersionPair,
                Task<Compatibility.Common.Model.PackageDetails>>(){
                    {
                        new Compatibility.Common.Model.PackageVersionPair
                        {
                            PackageId = "Newtonsoft.Json",
                            Version = "13.0.1",
                            PackageSourceType = Compatibility.Common.Model.PackageSourceType.NUGET
                        },
                        task.Task
                    },

                    {
                        new Compatibility.Common.Model.PackageVersionPair
                            {
                                PackageId = "Microsoft.SourceLink.GitHub",
                                Version = "1.0.0",
                                PackageSourceType = Compatibility.Common.Model.PackageSourceType.NUGET
                            },
                            microsoft_sourcelink_github_task.Task
                    },

                    {
                        new Compatibility.Common.Model.PackageVersionPair
                            {
                                PackageId = "Nerdbank.GitVersioning",
                                Version = "3.4.231",
                                PackageSourceType = Compatibility.Common.Model.PackageSourceType.NUGET
                            },
                            nerdbank_gitVersioning_task.Task
                    },

                };

            _nuGetHandlerMock.Setup(handler => handler.GetNugetPackages(
                It.IsAny<List<Compatibility.Common.Model.PackageVersionPair>>()))
                .Returns((List<Compatibility.Common.Model.PackageVersionPair> input) =>
                    {
                        return getNugetPackages;
                    });


            _recommendationHandlerMock.Reset();
            _recommendationHandlerMock.Setup(handler => handler.GetApiRecommendation(It.IsAny<List<string>>()))
                .Returns((List<string> packageVersionPairs) =>
                {
                    return new Dictionary<string, Task<Compatibility.Common.Model.RecommendationDetails>>();
                });

            _httpService.Reset();
            _httpService.Setup(handler => handler.DownloadS3FileAsync(It.IsAny<string>()))
            .Returns(async (string file2Download) => {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("https://s3.us-west-2.amazonaws.com/preprod.aws.portingassistant.service.datastore.uswest2/");
                switch (file2Download) {
                    case "microsoft.sourcelink.github/microsoft.sourcelink.github-1.0.0-api.json.gz":
                        return await client.GetStreamAsync("microsoft.sourcelink.github/microsoft.sourcelink.github-1.0.0-api.json.gz");
                    case "nerdbank.gitversioning/nerdbank.gitversioning-3.4.231-api.json.gz"://   Nerdbank.GitVersioning - 3.4.231
                        return await client.GetStreamAsync("nerdbank.gitversioning/nerdbank.gitversioning-3.4.231-api.json.gz");
                    case "newtonsoft.json/newtonsoft.json-13.0.1-api.json.gz":
                        return await client.GetStreamAsync("newtonsoft.json/newtonsoft.json-13.0.1-api.json.gz");
                    default:
                        return await client.GetStreamAsync("newtonsoft.json/newtonsoft.json-13.0.1-api.json.gz");
                }
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

            return new PortingAssistantAnalysisHandler(_loggerMock.Object, _compatibilityCheckerHandlerMock.Object, _cacheServiceMock.Object);
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
            var result = _analysisHandler.AnalyzeSolution(_solutionFile, _projectPaths, assessmentType: AssessmentType.FullAssessment);
            Task.WaitAll(result);

            var projectAnalysisResult = result.Result.Values.First();
            Task.WaitAll(projectAnalysisResult.PackageAnalysisResults.Values.ToArray());
            var packageAnalysisResult = projectAnalysisResult.PackageAnalysisResults.FirstOrDefault(p => p.Key.PackageId == "Newtonsoft.Json").Value?.Result;

            Assert.AreEqual(package, packageAnalysisResult.PackageVersionPair);
            Assert.AreEqual(Model.Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
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
            Assert.AreEqual(Model.Compatibility.COMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
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
            var result = _analysisHandler.AnalyzeSolution(_vbSolutionFile, _vbProjectPaths, assessmentType: AssessmentType.FullAssessment);
            Task.WaitAll(result);

            var projectAnalysisResult = result.Result.Values.First();
            Task.WaitAll(projectAnalysisResult.PackageAnalysisResults.Values.ToArray());
            var packageAnalysisResult = projectAnalysisResult.PackageAnalysisResults.First(p => p.Key.PackageId == "Newtonsoft.Json").Value.Result;

            Assert.AreEqual(package, packageAnalysisResult.PackageVersionPair);
            Assert.AreEqual(Model.Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
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
            Assert.AreEqual(Model.Compatibility.COMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
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
        public void ProjectCompatibilityResultToStringTest()
        {
            var result = _analysisHandler.AnalyzeSolution(_solutionFile, _projectPaths, assessmentType: AssessmentType.FullAssessment);
            Task.WaitAll(result);

            var projectAnalysisResult = result.Result.Values.First();
            Task.WaitAll(projectAnalysisResult.PackageAnalysisResults.Values.ToArray());
            var projectCompatibilityResult = projectAnalysisResult.ProjectCompatibilityResult;

            var str = projectCompatibilityResult.ToString();
            /*
             * my test data
            "Analyzed Project Compatibilities " +
                "for /Users/xueningl/git/porting-assistant-dotnet-client/tests/PortingAssistant." +
                "Client.UnitTests/bin/Debug/net6.0/TestXml/SolutionWithApi/testproject/testproject.csproj:\n" +
                "Annotation: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0\n" +
                "Method: Compatible:1, Incompatible:1, Unknown:0, Deprecated:0, Actions:0\n" +
                "Declaration: Compatible:0, Incompatible:3, Unknown:2, Deprecated:0, Actions:0\n" +
                "Enum: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0\n" +
                "Struct: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0";


            Assert.AreEqual("Analyzed Project Compatibilities for " + _projectPaths[0] + ":" + Environment.NewLine +
                            "Annotation: Compatible:0, Incompatible:0, Unknown:2, Deprecated:0, Actions:0" + Environment.NewLine +
                            "Method: Compatible:1, Incompatible:0, Unknown:1, Deprecated:0, Actions:0" + Environment.NewLine +
                            "Declaration: Compatible:1, Incompatible:0, Unknown:17, Deprecated:0, Actions:0" + Environment.NewLine +
                            "Enum: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0" + Environment.NewLine +
                            "Struct: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0" + Environment.NewLine, str);
            */
        }
        [Test]
        public async Task GetCompatibilityResultsWellDefinedSolutionSucceeds()
        {
            var package = new PackageVersionPair
            {
                PackageId = "Newtonsoft.Json",
                Version = "13.0.1",
                PackageSourceType = PackageSourceType.NUGET
            };

            //prepopulate analyzer results from codelyzer
            var analyzerResults = _analysisHandler.RunCoderlyzerAnalysis(_solutionFile, _projectPaths);
            var result = _analysisHandler.GetCompatibilityResults(_solutionFile, _projectPaths, await analyzerResults);


            var projectAnalysisResult = result.Values.First();
            Task.WaitAll(projectAnalysisResult.PackageAnalysisResults.Values.ToArray());
            var packageAnalysisResult = projectAnalysisResult.PackageAnalysisResults.FirstOrDefault(p => p.Key.PackageId == "Newtonsoft.Json").Value?.Result;

            Assert.AreEqual(package, packageAnalysisResult.PackageVersionPair);
            Assert.AreEqual(Model.Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
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
            Assert.AreEqual(Model.Compatibility.COMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
            Assert.AreEqual("13.0.2", apiAnalysisResult.Recommendations.RecommendedActions.First().Description);
        }

        [Test]
        public async Task VBGetCompatibilityResultsWellDefinedSolutionSucceeds()
        {
            var package = new PackageVersionPair
            {
                PackageId = "Newtonsoft.Json",
                Version = "13.0.1",
                PackageSourceType = PackageSourceType.NUGET
            };
            //prepopulate analyzer results from codelyzer
            var analyzerResults = _analysisHandler.RunCoderlyzerAnalysis(_vbSolutionFile, _vbProjectPaths);
            var result = _analysisHandler.GetCompatibilityResults(_vbSolutionFile, _vbProjectPaths, await analyzerResults);

            var projectAnalysisResult = result.Values.First();
            Task.WaitAll(projectAnalysisResult.PackageAnalysisResults.Values.ToArray());
            var packageAnalysisResult = projectAnalysisResult.PackageAnalysisResults.First(p => p.Key.PackageId == "Newtonsoft.Json").Value.Result;

            Assert.AreEqual(package, packageAnalysisResult.PackageVersionPair);
            Assert.AreEqual(Model.Compatibility.COMPATIBLE, packageAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
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
            Assert.AreEqual(Model.Compatibility.COMPATIBLE, apiAnalysisResult.CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
            Assert.AreEqual("13.0.2", apiAnalysisResult.Recommendations.RecommendedActions.First().Description);
        }

        [Test]
        public async Task GetCompatibilityResultsBadProjectDoesNotThrowException()
        {
            //prepopulate analyzer results from codelyzer
            var analyzerResults = _analysisHandler.RunCoderlyzerAnalysis(_solutionFile, new List<string> { "Sample.csproj" });
            var result = _analysisHandler.GetCompatibilityResults(_solutionFile, new List<string> { "Sample.csproj" }, await analyzerResults);
            Assert.IsNull(result.GetValueOrDefault("Sample.csproj", null));
        }

        [Test]
        public Task GetCompatibilityResultsNullPathThrowsException()
        {
            //prepopulate analyzer results from codelyzer
            var analyzerResults = _analysisHandler.RunCoderlyzerAnalysis(_solutionFile, new List<string> { "Sample.csproj" });
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var result = _analysisHandler.GetCompatibilityResults(null, _projectPaths, await analyzerResults);
            });
            return Task.CompletedTask;
        }
    }
}
