using System.Collections.Generic;
using System.Threading.Tasks;
using AwsCodeAnalyzer.Model;
using PortingAssistantApiAnalysis.Utils;
using PortingAssistant.NuGet;
using PortingAssistant.Model;
using Moq;
using NUnit.Framework;

namespace Tests.ApiAnalysis
{
    public class ResultsToInvocationsWithCompatibilityTest
    {
        private Mock<IPortingAssistantNuGetHandler> _handler;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _handler = new Mock<IPortingAssistantNuGetHandler>();
        }

        [SetUp]
        public void SetUp()
        {
            _handler.Reset();
            _handler.Setup(handler => handler.GetPackageDetails(It.IsAny<PackageVersionPair>()))
                .Returns((PackageVersionPair package) =>
                {
                    var task = new TaskCompletionSource<PackageDetails>();
                    task.SetResult(_packageDetails);
                    return task.Task;
                });
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
            Deprecated = false
        };

        [Test]
        public void NormalCaseTest()
        {
            var sourceFileToInvocations = new Dictionary<string, List<InvocationExpression>>
            {
                {
                    "file1", new List<InvocationExpression>
                    {
                        new MockInvocationExpressionModel("Newtonsoft.Json.JsonConvert.SerializeObject(object)", "namespace")
                    }
                }
            };

            var project = new ProjectDetails
            {
                PackageReferences = new List<PackageVersionPair>
                {
                    new PackageVersionPair {
                        PackageId = "namespace",
                        Version = "11.2"
                    }
                }
            };

            var result = InvocationExpressionModelToInvocations.Convert(
                sourceFileToInvocations, project, _handler.Object, new Dictionary<PackageVersionPair, Task<PackageDetails>>(), new Dictionary<string, Task<RecommendationDetails>>());

            Assert.AreEqual(1, result[0].ApiAnalysisResults.Count);
            Assert.AreEqual("11.2.0", result[0].ApiAnalysisResults[0].Invocation.Package.Version);
            Assert.AreEqual(Compatibility.COMPATIBLE, result[0].ApiAnalysisResults[0].CompatibilityResult);
            Assert.AreEqual("12.0.4", result[0].ApiAnalysisResults[0].ApiRecommendation.UpgradeVersion);
        }

        [Test]
        public void MultipleNuget()
        {
            var sourceFileToInvocations = new Dictionary<string, List<InvocationExpression>>
            {
                {
                    "file1", new List<InvocationExpression>
                    {
                        new MockInvocationExpressionModel("Newtonsoft.Json.JsonConvert.SerializeObject(object)", "namespace.namespace2.namespace3")
                    }
                }
            };

            var project = new ProjectDetails
            {
                PackageReferences = new List<PackageVersionPair>
                {
                    new PackageVersionPair {
                        PackageId = "namespace",
                        Version = "11.2"
                    },
                    new PackageVersionPair {
                        PackageId = "namespace.namespace2",
                        Version = "11.2"
                    }
                }
            };

            var result = InvocationExpressionModelToInvocations.Convert(
                sourceFileToInvocations, project, _handler.Object, new Dictionary<PackageVersionPair, Task<PackageDetails>>(), new Dictionary<string, Task<RecommendationDetails>>());

            Assert.AreEqual(1, result[0].ApiAnalysisResults.Count);
            Assert.AreEqual("11.2.0", result[0].ApiAnalysisResults[0].Invocation.Package.Version);
            Assert.AreEqual(Compatibility.COMPATIBLE, result[0].ApiAnalysisResults[0].CompatibilityResult);
            Assert.AreEqual("12.0.4", result[0].ApiAnalysisResults[0].ApiRecommendation.UpgradeVersion);
        }

        [Test]
        public void BadVersion()
        {
            var sourceFileToInvocations = new Dictionary<string, List<InvocationExpression>>
            {
                {
                    "file1", new List<InvocationExpression>
                    {
                        new MockInvocationExpressionModel("Newtonsoft.Json.JsonConvert.SerializeObject(object)", "namespace.namespace2.namespace3")
                    }
                }
            };

            var project = new ProjectDetails
            {
                PackageReferences = new List<PackageVersionPair>
                {
                    new PackageVersionPair {
                        PackageId = "namespace",
                        Version = "*"
                    },
                    new PackageVersionPair {
                        PackageId = "namespace.namespace2",
                        Version = "NOT_SEMVER"
                    }
                }
            };

            var result = InvocationExpressionModelToInvocations.Convert(
                sourceFileToInvocations, project, _handler.Object, new Dictionary<PackageVersionPair, Task<PackageDetails>>(), new Dictionary<string, Task<RecommendationDetails>>());

            Assert.AreEqual(1, result[0].ApiAnalysisResults.Count);
            Assert.AreEqual("namespace.namespace2", result[0].ApiAnalysisResults[0].Invocation.Package.PackageId);
            Assert.IsNull(result[0].ApiAnalysisResults[0].Invocation.Package.Version);
            Assert.AreEqual(Compatibility.INCOMPATIBLE, result[0].ApiAnalysisResults[0].CompatibilityResult);
            Assert.IsNull(result[0].ApiAnalysisResults[0].ApiRecommendation.UpgradeVersion);
        }
    }
}
