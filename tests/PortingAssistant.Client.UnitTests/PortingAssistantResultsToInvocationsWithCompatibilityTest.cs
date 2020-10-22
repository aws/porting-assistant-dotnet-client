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
        private Dictionary<PackageVersionPair, Task<PackageDetails>> packageResults;
        private Dictionary<string, Task<RecommendationDetails>> recommendationResults;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _handler = new Mock<IPortingAssistantNuGetHandler>();
        }

        [SetUp]
        public void SetUp()
        {
            _handler.Reset();
            _handler.Setup(handler => handler.GetNugetPackages(It.IsAny<List<PackageVersionPair>>(), ""))
                .Returns((List<PackageVersionPair> packages, string path) =>
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
            var recommendationTask = new TaskCompletionSource<RecommendationDetails>();

            packageTask.SetResult(_packageDetails);
            recommendationTask.SetResult(new RecommendationDetails());

            packageResults = new Dictionary<PackageVersionPair, Task<PackageDetails>>
            {
                {package, packageTask.Task }
            };

            recommendationResults = new Dictionary<string, Task<RecommendationDetails>>
            {
                {"Newtonsoft.Json", recommendationTask.Task }
            };
        }

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
        };

        [Test]
        public void NormalAnalyzeCaseTest()
        {
            var sourceFileToInvocations = new Dictionary<string, List<CodeEntityDetails>>
            {
                {
                    "file1", new UstList<CodeEntityDetails>
                    {
                       _codeEntityDetails
                    }
                }
            };

            var result = InvocationExpressionModelToInvocations.AnalyzeResults(
                sourceFileToInvocations, packageResults, recommendationResults);

            Assert.AreEqual(1, result.First().ApiAnalysisResults.Count);
            Assert.AreEqual("11.0.1", result.First().ApiAnalysisResults.First().CodeEntityDetails.Package.Version);
            Assert.AreEqual(Compatibility.COMPATIBLE, result.First().ApiAnalysisResults.First().CompatibilityResults.GetValueOrDefault(ApiCompatiblity.DEFAULT_TARGET).Compatibility);
            Assert.AreEqual("12.0.3", result[0].ApiAnalysisResults[0].Recommendations.RecommendedActions.First().Description);
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

            var result = InvocationExpressionModelToInvocations.AnalyzeResults(
                sourceFileToInvocations, new Dictionary<PackageVersionPair, Task<PackageDetails>>(), recommendationResults);

            Assert.AreEqual(1, result.First().ApiAnalysisResults.Count);
            Assert.AreEqual("11.0.1", result.First().ApiAnalysisResults.First().CodeEntityDetails.Package.Version);
            Assert.AreEqual(Compatibility.UNKNOWN, result.First().ApiAnalysisResults.First().CompatibilityResults.GetValueOrDefault(ApiCompatiblity.DEFAULT_TARGET).Compatibility);
            Assert.IsNull(result[0].ApiAnalysisResults[0].Recommendations.RecommendedActions.First().Description);
        }


        [Test]
        public void NormalCaseTest()
        {
            var sourceFileToInvocations = new Dictionary<string, UstList<InvocationExpression>>
             {
                 {
                     "file1", new UstList<InvocationExpression>
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

            var result = InvocationExpressionModelToInvocations.Convert(
                sourceFileToInvocations, project);

            Assert.AreEqual(1, result["file1"].Count);
            Assert.AreEqual("1.2.0", result["file1"][0].Package.Version);
        }


        [Test]
        public void MultipleNuget()
        {
            var sourceFileToInvocations = new Dictionary<string, UstList<InvocationExpression>>
            {
                {
                    "file1", new UstList<InvocationExpression>
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

            var result = InvocationExpressionModelToInvocations.Convert(
                sourceFileToInvocations, project);

            Assert.AreEqual(1, result["file1"].Count);
            Assert.AreEqual("1.2.0", result["file1"][0].Package.Version);

        }

        [Test]
        public void NugetDependencies()
        {
            var sourceFileToInvocations = new Dictionary<string, UstList<InvocationExpression>>
             {
                 {
                     "file1", new UstList<InvocationExpression>
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

            var result = InvocationExpressionModelToInvocations.Convert(
                sourceFileToInvocations, project);

            Assert.AreEqual(1, result["file1"].Count);
            Assert.AreEqual("1.2.0", result["file1"][0].Package.Version);
        }

        [Test]
        public void BadVersion()
        {
            var sourceFileToInvocations = new Dictionary<string, UstList<InvocationExpression>>
             {
                 {
                     "file1", new UstList<InvocationExpression>
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

            var result = InvocationExpressionModelToInvocations.Convert(
                sourceFileToInvocations, project);

            Assert.AreEqual(1, result["file1"].Count);
            Assert.AreEqual("namespace.namespace2", result["file1"][0].Package.PackageId);
            Assert.AreEqual("NOT_SEMVER", result["file1"][0].Package.Version);

        }
    }
}
