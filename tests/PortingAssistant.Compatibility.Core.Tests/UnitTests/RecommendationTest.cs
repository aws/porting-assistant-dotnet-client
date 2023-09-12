using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using PortingAssistant.Compatibility.Common.Utils;
using Microsoft.Extensions.Logging;
using PortingAssistant.Compatibility.Core.Checkers;

namespace PortingAssistant.Compatibility.Core.Tests.UnitTests
{
    public class RecommendationTest
    {
        private Mock<IHttpService> _httpService;
        private ICompatibilityCheckerRecommendationHandler _compatibilityCheckerRecommendationHandler;

        private readonly RecommendationFileDetails _recommendationDetails = new RecommendationFileDetails
        {
            Name = "System.Web.Configuration",
            Version = "1.0.0",
            Packages = new Packages[]
            {
                new Packages()
                {
                    Type = "SDK",
                    Name = "System.Web.Configuration"
                }
                
            },
            Recommendations = new RecommendationModel[]
            {
                new RecommendationModel
                {
                    Type = "Method",
                    Value = "System.Web.Configuration.BrowserCapabilitiesFactory.OperaminiProcessBrowsers(bool, System.Collections.Specialized.NameValueCollection, System.Web.HttpBrowserCapabilities)",
                    Name = "",
                    KeyType = "",
                    RecommendedActions = new RecommendedActionModel[]
                    {
                        new RecommendedActionModel()
                        {
                            Source = "Amazon",
                            Preferred = "yes",
                            TargetFrameworks = new SortedSet<string>()
                            {
                                "netframework4.5",
                                "netcoreapp3.1"
                            },
                            Description = "System.Web (AKA classic ASP.NET) won't be ported to .NET Core. See https://aka.ms/unsupported-netfx-api.",
                            Actions = Array.Empty<ActionFileActions>()
                        },
                        new RecommendedActionModel()
                        {
                            Source = "Amazon",
                            Preferred = "yes",
                            TargetFrameworks = new SortedSet<string>()
                            {
                                "net5.0"
                            },
                            Description = "No Recommendation in net5.0 now",
                            Actions = Array.Empty<ActionFileActions>()
                        }
                    }
                }

            }
        };

        private readonly CompatibilityResult compatibilityResult = new CompatibilityResult
        {
            Compatibility = Common.Model.Compatibility.INCOMPATIBLE,
            CompatibleVersions = new List<string>()
        };

        private readonly IEnumerable<string> _namespaces = new List<string>() { "System.Web.Configuration" };

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _httpService = new Mock<IHttpService>();
        }

        [SetUp]
        public void Setup()
        { 
            _compatibilityCheckerRecommendationHandler = new CompatibilityCheckerRecommendationHandler(
                _httpService.Object, Mock.Of<ILogger<CompatibilityCheckerRecommendationHandler>>());
        }


        [Test]
        public async Task GetRecommendationWithNamespace()
        {
            _httpService
                .Setup(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()))
                .Returns(async (string key) =>
                {
                    await Task.Delay(1);
                    var stream = new MemoryStream();
                    var writer = new StreamWriter(stream);
                    string test = null;
                    test = JsonConvert.SerializeObject(_recommendationDetails);
                    writer.Write(test);
                    writer.Flush();
                    stream.Position = 0;
                    var outputStream = new MemoryStream();
                    stream.CopyTo(outputStream);
                    outputStream.Position = 0;
                    return outputStream;
                });

            var resultTasks = await _compatibilityCheckerRecommendationHandler.GetRecommendationFileAsync( _namespaces);

            Assert.AreEqual(_recommendationDetails.Name, resultTasks.Values.First().Name);
            Assert.AreEqual(_recommendationDetails.Version, resultTasks.Values.First().Version);
            Assert.AreEqual(
                _recommendationDetails.Recommendations.Length,
                resultTasks.Values.First().Recommendations.Length);
            Assert.AreEqual(
                _recommendationDetails.Recommendations.First().Value,
                resultTasks.Values.First().Recommendations.First().Value);
            Assert.AreEqual(
                _recommendationDetails.Recommendations.First().RecommendedActions.First().Description,
                resultTasks.Values.First().Recommendations.First().RecommendedActions.First().Description);
        }

        [Test]
        public async Task TestUpgradeStrategy()
        {
            _httpService
                .Setup(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()))
                .Returns(async (string key) =>
                {
                    await Task.Delay(1);
                    var stream = new MemoryStream();
                    var writer = new StreamWriter(stream);
                    string test = null;
                    test = JsonConvert.SerializeObject(_recommendationDetails);
                    writer.Write(test);
                    writer.Flush();
                    stream.Position = 0;
                    var outputStream = new MemoryStream();
                    stream.CopyTo(outputStream);
                    outputStream.Position = 0;
                    return outputStream;
                });

            var apiMethod = "System.Web.Configuration.BrowserCapabilitiesFactory.OperaminiProcessBrowsers(bool, System.Collections.Specialized.NameValueCollection, System.Web.HttpBrowserCapabilities)";
            var description = "System.Web (AKA classic ASP.NET) won't be ported to .NET Core. See https://aka.ms/unsupported-netfx-api.";
            var resultTasks = await _compatibilityCheckerRecommendationHandler.GetRecommendationFileAsync (_namespaces);
            var recommendationDetails = resultTasks.Values.First();
            var recommendationActionDetails = new RecommendationActionFileDetails();
            var actions = recommendationDetails.Recommendations.First().RecommendedActions.SelectMany(a => a.Actions).ToList();
            var recommendation = ApiCompatiblity.UpgradeStrategy(compatibilityResult, apiMethod, recommendationDetails, recommendationActionDetails, actions, "netcoreapp3.1");
            Assert.AreEqual(RecommendedActionType.ReplaceApi, recommendation.RecommendedActionType);
            Assert.AreEqual(description, recommendation.Description);
            recommendation = ApiCompatiblity.UpgradeStrategy(compatibilityResult, apiMethod, recommendationDetails, recommendationActionDetails, actions, "net5.0");
            Assert.AreEqual(RecommendedActionType.ReplaceApi, recommendation.RecommendedActionType);
            Assert.AreEqual("No Recommendation in net5.0 now", recommendation.Description);
            recommendation = ApiCompatiblity.UpgradeStrategy(compatibilityResult, apiMethod, recommendationDetails, recommendationActionDetails, actions, "xxxx");
            Assert.AreEqual(RecommendedActionType.NoRecommendation, recommendation.RecommendedActionType);
        }

        [Test]
        public void TestPackageCompatibilityResult()
        {
            var versions = new SortedSet<string>
            {
                "1.0.0",
                "1.0.1-beta",
                "2.0.0",
            };
            var packageVersionPair = new PackageVersionPair
            {
                PackageId = "MyNugetPackage",
                PackageSourceType = PackageSourceType.NUGET,
                Version = "1.0.0"
            };
            var packageDetails = new PackageDetails
            {
                Name = "MyNugetPackage",
                Versions = versions,
                Targets = new Dictionary<string, SortedSet<string>>
                {
                    { "netcoreapp3.1",  versions},
                    { "net6.0",  versions}
                }
            };

            var compatResults = PackageCompatibility.IsCompatibleAsync(Task.FromResult(packageDetails), packageVersionPair, Mock.Of<ILogger>());
            var actions = new List<ActionFileActions>();
            var recommendation = PackageCompatibility.GetPackageAnalysisResult(compatResults.Result, packageVersionPair, "netcoreapp3.1", actions, assessmentType: AssessmentType.FullAssessment);

            Assert.AreEqual(2, compatResults.Result.CompatibleVersions.Count);
            Assert.AreEqual(1, recommendation.CompatibilityResults["netcoreapp3.1"].CompatibleVersions.Count);
            //Assert.AreEqual("2.0.0", recommendation.Recommendations.RecommendedActions[0].Description);
            // No recommandation attached
            Assert.AreEqual(0, recommendation.Recommendations.RecommendedActions.Count);
        }

        [Test]
        public void TestIsCompatibleAsync_ReturnsIncompatible_PackageVersionNotInTargetVersions()
        {
            var versions = new SortedSet<string>
            {
                "4.0.0",
                "4.5.0"
            };
            var packageVersionPair = new PackageVersionPair
            {
                PackageId = "System.DirectoryServices",
                PackageSourceType = PackageSourceType.NUGET,
                Version = "5.0.0"
            };
            var packageDetails = new PackageDetails
            {
                Name = "System.DirectoryServices",
                Versions = versions,
                Targets = new Dictionary<string, SortedSet<string>>
                {
                    { "net5.0",  versions }
                }
            };

            var compatResults = PackageCompatibility.IsCompatibleAsync(Task.FromResult(packageDetails), packageVersionPair, Mock.Of<ILogger>());

            Assert.AreEqual(0, compatResults.Result.CompatibleVersions.Count);
            Assert.AreEqual(Common.Model.Compatibility.INCOMPATIBLE, compatibilityResult.Compatibility);
        }

        [Test]
        public async Task GetRecommendation_NamespaceNotFound_Return404Exception()
        {
            _httpService
                .Setup(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("404 not found"));

            var loggerMock = new Mock<ILambdaLogger>();

            IEnumerable<string> namepaces = new List<string>() { "test.namespace" };
            var resultTasks = await _compatibilityCheckerRecommendationHandler.GetRecommendationFileAsync(namepaces);

            Assert.AreEqual(1, resultTasks.Count);
            Assert.IsNull(resultTasks[namepaces.First()]);
            loggerMock.Verify(mock => mock.LogInformation(It.IsAny<string>()), Times.Once);
            loggerMock.Verify(mock => mock.LogError(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GetRecommendation_ReturnOtherException()
        {
            _httpService
                .Setup(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("error"));

            var loggerMock = new Mock<ILambdaLogger>();

            IEnumerable<string> namepaces = new List<string>() { "test.namespace" };
            var resultTasks = await _compatibilityCheckerRecommendationHandler.GetRecommendationFileAsync( namepaces);

            Assert.AreEqual(1, resultTasks.Count);
            Assert.IsNull(resultTasks[namepaces.First()]);
            loggerMock.Verify(mock => mock.LogInformation(It.IsAny<string>()), Times.Never);
            loggerMock.Verify(mock => mock.LogError(It.IsAny<string>()), Times.Once);
        }
    }

}
