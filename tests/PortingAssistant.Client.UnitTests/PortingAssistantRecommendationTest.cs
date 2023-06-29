using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using PortingAssistant.Client.Analysis.Utils;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.NuGet;
using PortingAssistant.Client.NuGet.Interfaces;
namespace PortingAssistant.Client.UnitTests
{
    public class PortingAssistantRecommendationTest
    {
        private Mock<IHttpService> _httpService;
        private IPortingAssistantRecommendationHandler _portingAssistantRecommendationHandler;

        private readonly RecommendationDetails _recommendationDetails = new RecommendationDetails
        {
            Name = "System.Web.Configuration",
            Version = "1.0.0",
            Recommendations = new RecommendationModel[]
            {
                new RecommendationModel
                {
                    Type = "Method",
                    Value = "System.Web.Configuration.BrowserCapabilitiesFactory.OperaminiProcessBrowsers(bool, System.Collections.Specialized.NameValueCollection, System.Web.HttpBrowserCapabilities)",
                    RecommendedActions = new RecommendedActionModel[]
                    {
                        new RecommendedActionModel()
                        {
                            Source = "Amazon",
                            Preferred = "yes",
                            TargetFrameworks = new SortedSet<string>()
                            {
                                "netframework45",
                                "netcore31"
                            },
                            Description = "System.Web (AKA classic ASP.NET) won't be ported to .NET Core. See https://aka.ms/unsupported-netfx-api.",
                            Actions = Array.Empty<Actions>()
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
                            Actions = Array.Empty<Actions>()
                        }
                    }
                }

            }
        };

        private readonly CompatibilityResult compatibilityResult = new CompatibilityResult
        {
            Compatibility = Compatibility.INCOMPATIBLE,
            CompatibleVersions = new List<string>()
        };

        private readonly Dictionary<string, string> _manifest = new Dictionary<string, string>() { { "System.Web.Configuration", "system.web.configuration.recommendation.json" } };
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _httpService = new Mock<IHttpService>();
        }

        [SetUp]
        public void Setup()
        {
            _httpService.Reset();

            _httpService
                .Setup(transfer => transfer.DownloadGitHubFileAsync(It.IsAny<string>()))
                .Returns(async (string key) =>
                {
                    await Task.Delay(1);
                    var stream = new MemoryStream();
                    var writer = new StreamWriter(stream);
                    string test = null;
                    if (key.Equals("data/namespaces.recommendation.lookup.json"))
                    {
                        test = JsonConvert.SerializeObject(_manifest);
                    }
                    else
                    {
                        test = JsonConvert.SerializeObject(_recommendationDetails);
                    }
                    writer.Write(test);
                    writer.Flush();
                    stream.Position = 0;
                    var outputStream = new MemoryStream();
                    stream.CopyTo(outputStream);
                    outputStream.Position = 0;
                    return outputStream;
                });



            _portingAssistantRecommendationHandler = new PortingAssistantRecommendationHandler(
                _httpService.Object,
                NullLogger<PortingAssistantRecommendationHandler>.Instance
                );
        }


        [Test]
        public void GetRecommendationWithNamespace()
        {
            var namespaces = new List<string>() { "System.Web.Configuration" };

            var resultTasks = _portingAssistantRecommendationHandler.GetApiRecommendation(namespaces);
            Task.WaitAll(resultTasks.Values.ToArray());

            Assert.AreEqual(_recommendationDetails.Name, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(_recommendationDetails.Version, resultTasks.Values.First().Result.Version);
            Assert.AreEqual(
                _recommendationDetails.Recommendations.Length,
                resultTasks.Values.First().Result.Recommendations.Length);
            Assert.AreEqual(
                _recommendationDetails.Recommendations.First().Value,
                resultTasks.Values.First().Result.Recommendations.First().Value);
            Assert.AreEqual(
                _recommendationDetails.Recommendations.First().RecommendedActions.First().Description,
                resultTasks.Values.First().Result.Recommendations.First().RecommendedActions.First().Description);
        }

        [Test]
        public void TestUpgradeStrategy()
        {
            var namespaces = new List<string>() { "System.Web.Configuration" };

            var apiMethod = "System.Web.Configuration.BrowserCapabilitiesFactory.OperaminiProcessBrowsers(bool, System.Collections.Specialized.NameValueCollection, System.Web.HttpBrowserCapabilities)";
            var description = "System.Web (AKA classic ASP.NET) won't be ported to .NET Core. See https://aka.ms/unsupported-netfx-api.";
            var resultTasks = _portingAssistantRecommendationHandler.GetApiRecommendation(namespaces).GetValueOrDefault("System.Web.Configuration");
            var recommendation = ApiCompatiblity.UpgradeStrategy(compatibilityResult, apiMethod, resultTasks, "netcore31");
            Assert.AreEqual(RecommendedActionType.ReplaceApi, recommendation.RecommendedActionType);
            Assert.AreEqual(description, recommendation.Description);
            recommendation = ApiCompatiblity.UpgradeStrategy(compatibilityResult, apiMethod, resultTasks, "net5.0");
            Assert.AreEqual(RecommendedActionType.ReplaceApi, recommendation.RecommendedActionType);
            Assert.AreEqual("No Recommendation in net5.0 now", recommendation.Description);
            recommendation = ApiCompatiblity.UpgradeStrategy(compatibilityResult, apiMethod, resultTasks, "xxxx");
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

            var compatResults = PackageCompatibility.IsCompatibleAsync(Task.FromResult(packageDetails), packageVersionPair, NullLogger.Instance);
            var recommendation = PackageCompatibility.GetPackageAnalysisResult(compatResults, packageVersionPair, "netcoreapp3.1").Result;

            Assert.AreEqual(2, compatResults.Result.CompatibleVersions.Count);
            Assert.AreEqual(1, recommendation.CompatibilityResults["netcoreapp3.1"].CompatibleVersions.Count);
            Assert.AreEqual("2.0.0", recommendation.Recommendations.RecommendedActions[0].Description);
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

            var compatResults = PackageCompatibility.IsCompatibleAsync(Task.FromResult(packageDetails), packageVersionPair, NullLogger.Instance);

            Assert.AreEqual(0, compatResults.Result.CompatibleVersions.Count);
            Assert.AreEqual(Compatibility.INCOMPATIBLE, compatibilityResult.Compatibility);
        }

        [Test]
        public void PortingAssistantModelsTest()
        {
            var portingAction1 = new PortingAction();

            portingAction1.TextSpan = null;
            portingAction1.RecommendedAction = null;
            portingAction1.TargetFramework = null;

            var portingAction2 = new PortingAction();

            portingAction2.TextSpan = null;
            portingAction2.RecommendedAction = null;
            portingAction2.TargetFramework = null;

            Assert.True(portingAction1.Equals(portingAction2));
            Assert.AreEqual(portingAction1.GetHashCode(), portingAction2.GetHashCode());

            var actions1 = new Actions();

            actions1.Type = null;
            actions1.Value = null;

            var actions2 = new Actions();

            actions2.Type = null;
            actions2.Value = null;

            Assert.True(actions1.Equals(actions2));
            Assert.AreEqual(actions1.GetHashCode(), actions2.GetHashCode());

            var packages1 = new Model.Packages();

            packages1.Type = null;
            packages1.Name = null;

            var packages2 = new Model.Packages();

            packages2.Type = null;
            packages2.Name = null;

            Assert.True(packages1.Equals(packages2));
            Assert.AreEqual(packages1.GetHashCode(), packages2.GetHashCode());
        }
    }
}
