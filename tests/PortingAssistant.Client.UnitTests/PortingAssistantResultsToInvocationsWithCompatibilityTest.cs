using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codelyzer.Analysis;
using Codelyzer.Analysis.Model;
using Moq;
using NUnit.Framework;
using PortingAssistant.Client.Analysis.Utils;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.NuGet;
using TextSpan = PortingAssistant.Client.Model.TextSpan;

namespace PortingAssistant.Client.Tests
{

    public class PortingAssistantResultsToInvocationsWithCompatibilityTest
    {
        private Mock<IPortingAssistantNuGetHandler> _handler;
        private Dictionary<PackageVersionPair, Task<PackageDetails>> _packageResults;
        private Dictionary<string, Task<RecommendationDetails>> _recommendationResults;
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
                    MethodSignature = "Newtonsoft.Json.JsonConvert.SerializeObject(object)",
                    Targets = new Dictionary<string, SortedSet<string>>
                    {
                        {
                            "netcoreapp3.1", new SortedSet<string> { "10.2.0", "12.0.3", "12.0.4" }
                        },
                        {
                            "net6.0", new SortedSet<string> { "10.2.0", "12.0.3", "12.0.4" }
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
                },
            },
            License = new LicenseDetails
            {
                License = new Dictionary<string, SortedSet<string>>
                {
                    { "MIT", new SortedSet<string> { "12.0.3", "12.0.4" } }
                }
            },
            IsDeprecated = false
        };

        private readonly CodeEntityDetails _codeEntityDetails = new CodeEntityDetails
        {
            Name = "JsonConvert.SerializeObject",
            OriginalDefinition = "Newtonsoft.Json.JsonConvert.SerializeObject(object)",
            Namespace = "Newtonsoft.Json",
            Package = new PackageVersionPair
            {
                PackageId = "Newtonsoft.Json",
                Version = "11.0.1"
            },
            TextSpan = new TextSpan(),
            CodeEntityType = CodeEntityType.Method
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _handler = new Mock<IPortingAssistantNuGetHandler>();
        }

        [SetUp]
        public void SetUp()
        {
            _handler.Reset();
            _handler.Setup(handler => handler.GetNugetPackages(It.IsAny<List<PackageVersionPair>>(), "", It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns((List<PackageVersionPair> packages, string path, bool isIncremental, bool incrementalRefresh) =>
                {
                    var task = new TaskCompletionSource<PackageDetails>();
                    task.SetResult(_packageDetails);
                    return new Dictionary<PackageVersionPair, Task<PackageDetails>> {
                        {packages.First(), task.Task }
                    };
                });
            var package = new PackageVersionPair
            {
                PackageId = "Newtonsoft.Json",
                Version = "11.0.1"
            };

            var packageTask = new TaskCompletionSource<PackageDetails>();
            packageTask.SetResult(_packageDetails);
            _packageResults = new Dictionary<PackageVersionPair, Task<PackageDetails>>
            {
                {package, packageTask.Task }
            };

            var recommendationTask = new TaskCompletionSource<RecommendationDetails>();
            recommendationTask.SetResult(new RecommendationDetails());
            _recommendationResults = new Dictionary<string, Task<RecommendationDetails>>
            {
                {"Newtonsoft.Json", recommendationTask.Task }
            };
        }

        [Test]
        public void ApiAnalysis_Returns_Compatible_When_ApiVersion_Is_LargerThanAllCompatibleVersions_And_Has_A_LowerCompatibleVersionWithSameMajor()
        {
            var sourceFileToInvocations = new Dictionary<string, List<CodeEntityDetails>>
            {
                {
                    "file1", new UstList<CodeEntityDetails>
                    {
                        new CodeEntityDetails
                        {
                            Name = "JsonConvert.SerializeObject",
                            OriginalDefinition = "Newtonsoft.Json.JsonConvert.SerializeObject(object)",
                            Namespace = "Newtonsoft.Json",
                            Package = new PackageVersionPair
                            {
                                PackageId = "Newtonsoft.Json",
                                Version = "12.0.5"
                            },
                            TextSpan = new TextSpan(),
                            CodeEntityType = CodeEntityType.Method
                        }
                    }
                }
            };

            var package = new PackageVersionPair
            {
                PackageId = "Newtonsoft.Json",
                Version = "12.0.5"
            };

            var packageTask = new TaskCompletionSource<PackageDetails>();

            packageTask.SetResult(_packageDetails);
            _packageResults = new Dictionary<PackageVersionPair, Task<PackageDetails>>
            {
                { package, packageTask.Task }
            };

            var results = CodeEntityModelToCodeEntities.AnalyzeResults(
                sourceFileToInvocations, _packageResults, _recommendationResults, new Dictionary<string, List<RecommendedAction>>());

            var apiAnalysisResults = results.First().ApiAnalysisResults;
            Assert.AreEqual("12.0.5", apiAnalysisResults.First().CodeEntityDetails.Package.Version);
            Assert.AreEqual(Compatibility.COMPATIBLE, apiAnalysisResults.First().CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
            Assert.AreEqual(RecommendedActionType.NoRecommendation, apiAnalysisResults[0].Recommendations.RecommendedActions.First().RecommendedActionType);
        }

        [Test]
        public void ApiAnalysis_Returns_Incompatible_When_ApiVersion_Is_LessThan_All_CompatibleVersions()
        {
            var sourceFileToInvocations = new Dictionary<string, List<CodeEntityDetails>>
            {
                {
                    "file1", new UstList<CodeEntityDetails>
                    {
                        new CodeEntityDetails
                        {
                            Name = "JsonConvert.SerializeObject",
                            OriginalDefinition = "Newtonsoft.Json.JsonConvert.SerializeObject(object)",
                            Namespace = "Newtonsoft.Json",
                            Package = new PackageVersionPair
                            {
                                PackageId = "Newtonsoft.Json",
                                Version = "10.1.0"
                            },
                            TextSpan = new TextSpan(),
                            CodeEntityType = CodeEntityType.Method
                        }
                    }
                }
            };

            var package = new PackageVersionPair
            {
                PackageId = "Newtonsoft.Json",
                Version = "10.1.0"
            };

            var packageTask = new TaskCompletionSource<PackageDetails>();

            packageTask.SetResult(_packageDetails);
            _packageResults = new Dictionary<PackageVersionPair, Task<PackageDetails>>
            {
                {package, packageTask.Task }
            };

            var results = CodeEntityModelToCodeEntities.AnalyzeResults(
                sourceFileToInvocations, _packageResults, _recommendationResults, new Dictionary<string, List<RecommendedAction>>());

            var apiAnalysisResults = results.First().ApiAnalysisResults;
            Assert.AreEqual("10.1.0", apiAnalysisResults.First().CodeEntityDetails.Package.Version);
            Assert.AreEqual(Compatibility.INCOMPATIBLE, apiAnalysisResults.First().CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
            Assert.AreEqual("10.2.0", apiAnalysisResults[0].Recommendations.RecommendedActions.First().Description);
        }

        [Test]
        public void ApiAnalysis_Returns_Compatible_When_ApiVersion_Matches_A_CompatibleVersion()
        {
            var sourceFileToInvocations = new Dictionary<string, List<CodeEntityDetails>>
            {
                {
                    "file1", new UstList<CodeEntityDetails>
                    {
                        new CodeEntityDetails
                        {
                            Name = "JsonConvert.SerializeObject",
                            OriginalDefinition = "Newtonsoft.Json.JsonConvert.SerializeObject(object)",
                            Namespace = "Newtonsoft.Json",
                            Package = new PackageVersionPair
                            {
                                PackageId = "Newtonsoft.Json",
                                Version = "10.2.0"
                            },
                            TextSpan = new TextSpan(),
                            CodeEntityType = CodeEntityType.Method
                        }
                    }
                }
            };

            var package = new PackageVersionPair
            {
                PackageId = "Newtonsoft.Json",
                Version = "10.2.0"
            };

            var packageTask = new TaskCompletionSource<PackageDetails>();

            packageTask.SetResult(_packageDetails);
            _packageResults = new Dictionary<PackageVersionPair, Task<PackageDetails>>
            {
                {package, packageTask.Task }
            };

            var results = CodeEntityModelToCodeEntities.AnalyzeResults(
                sourceFileToInvocations, _packageResults, _recommendationResults, new Dictionary<string, List<RecommendedAction>>());

            var apiAnalysisResults = results.First().ApiAnalysisResults;
            Assert.AreEqual("10.2.0", apiAnalysisResults.First().CodeEntityDetails.Package.Version);
            Assert.AreEqual(Compatibility.COMPATIBLE, apiAnalysisResults.First().CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
            Assert.AreEqual("12.0.3", apiAnalysisResults[0].Recommendations.RecommendedActions.First().Description);
        }

        [Test]
        public void NoPackageResultsTest()
        {
            var sourceFileToInvocations = new Dictionary<string, List<CodeEntityDetails>>
            {
                {
                    "file1", new List<CodeEntityDetails>
                    {
                       _codeEntityDetails
                    }
                }
            };

            var result = CodeEntityModelToCodeEntities.AnalyzeResults(
                sourceFileToInvocations, new Dictionary<PackageVersionPair, Task<PackageDetails>>(), _recommendationResults, new Dictionary<string, List<RecommendedAction>>());

            Assert.AreEqual(1, result.First().ApiAnalysisResults.Count);
            Assert.AreEqual("11.0.1", result.First().ApiAnalysisResults.First().CodeEntityDetails.Package.Version);
            Assert.AreEqual(Compatibility.UNKNOWN, result.First().ApiAnalysisResults.First().CompatibilityResults.GetValueOrDefault(DEFAULT_TARGET).Compatibility);
            Assert.IsNull(result[0].ApiAnalysisResults[0].Recommendations.RecommendedActions.First().Description);
        }

        [Test]
        public void NormalCaseTest()
        {
            var sourceFileToInvocations = new Dictionary<string, UstList<UstNode>>
             {
                 {
                     "file1", new UstList<UstNode>
                     {
                         new MockInvocationExpressionModel("definition", "namespace", "test")
                     }
                 }
             };

            var project = new AnalyzerResult
            {
                ProjectResult = new ProjectWorkspace("")
                {
                    ExternalReferences = new ExternalReferences
                    {
                        NugetReferences = new List<ExternalReference>
                         {
                             new ExternalReference
                             {
                                 AssemblyLocation = "test.dll",
                                 Identity = "namespace",
                                 Version = "1.2"
                             }
                         }
                    }
                },
                OutputJsonFilePath = null,
                ProjectBuildResult = null
            };

            var result = CodeEntityModelToCodeEntities.Convert(
                sourceFileToInvocations, project);

            Assert.AreEqual(1, result["file1"].Count);
            Assert.AreEqual("1.2.0", result["file1"][0].Package.Version);
        }


        [Test]
        public void MultipleNuget()
        {
            var sourceFileToInvocations = new Dictionary<string, UstList<UstNode>>
            {
                {
                    "file1", new UstList<UstNode>
                    {
                        new MockInvocationExpressionModel("Newtonsoft.Json.JsonConvert.SerializeObject(object)", "namespace.namespace2.namespace3", "test")
                    }
                }
            };

            var project = new AnalyzerResult
            {
                ProjectResult = new ProjectWorkspace("")
                {
                    ExternalReferences = new ExternalReferences
                    {
                        NugetReferences = new List<ExternalReference>
                         {
                             new ExternalReference
                             {
                                 AssemblyLocation = "test.dll",
                                 Identity = "namespace",
                                 Version = "1.2"
                             },
                             new ExternalReference
                             {
                                 AssemblyLocation = "test2.dll",
                                 Identity = "namespace.namespace2",
                                 Version = "1.2"
                             }
                         }
                    }
                },
                OutputJsonFilePath = null,
                ProjectBuildResult = null
            };

            var result = CodeEntityModelToCodeEntities.Convert(
                sourceFileToInvocations, project);

            Assert.AreEqual(1, result["file1"].Count);
            Assert.AreEqual("1.2.0", result["file1"][0].Package.Version);

        }

        [Test]
        public void NugetDependencies()
        {
            var sourceFileToInvocations = new Dictionary<string, UstList<UstNode>>
             {
                 {
                     "file1", new UstList<UstNode>
                     {
                         new MockInvocationExpressionModel("definition", "namespace.namespace2.namespace3", "test")
                     }
                 }
             };

            var project = new AnalyzerResult
            {
                ProjectResult = new ProjectWorkspace("")
                {
                    ExternalReferences = new ExternalReferences
                    {
                        NugetDependencies = new List<ExternalReference>
                         {
                             new ExternalReference
                             {
                                 AssemblyLocation = "test.dll",
                                 Identity = "namespace",
                                 Version = "1.2"
                             },
                             new ExternalReference
                             {
                                 AssemblyLocation = "test2.dll",
                                 Identity = "namespace.namespace2",
                                 Version = "1.2"
                             }
                         }
                    }
                },
                OutputJsonFilePath = null,
                ProjectBuildResult = null
            };

            var result = CodeEntityModelToCodeEntities.Convert(
                sourceFileToInvocations, project);

            Assert.AreEqual(1, result["file1"].Count);
            Assert.AreEqual("1.2.0", result["file1"][0].Package.Version);
        }

        [Test]
        public void BadVersion()
        {
            var sourceFileToInvocations = new Dictionary<string, UstList<UstNode>>
             {
                 {
                     "file1", new UstList<UstNode>
                     {
                         new MockInvocationExpressionModel("definition", "namespace.namespace2.namespace3", "namespace2")
                     }
                 }
             };

            var project = new AnalyzerResult
            {
                ProjectResult = new ProjectWorkspace("")
                {
                    ExternalReferences = new ExternalReferences
                    {
                        NugetReferences = new List<ExternalReference>
                         {
                             new ExternalReference
                             {
                                 AssemblyLocation = "namespace.dll",
                                 Identity = "namespace",
                                 Version = "*"
                             },
                             new ExternalReference
                             {
                                 AssemblyLocation = "namespace2.dll",
                                 Identity = "namespace.namespace2",
                                 Version = "NOT_SEMVER"
                             }
                         }
                    }
                },
                OutputJsonFilePath = null,
                ProjectBuildResult = null
            };

            var result = CodeEntityModelToCodeEntities.Convert(
                sourceFileToInvocations, project);

            Assert.AreEqual(1, result["file1"].Count);
            Assert.AreEqual("namespace.namespace2", result["file1"][0].Package.PackageId);
            Assert.AreEqual("NOT_SEMVER", result["file1"][0].Package.Version);

        }
    }
}
