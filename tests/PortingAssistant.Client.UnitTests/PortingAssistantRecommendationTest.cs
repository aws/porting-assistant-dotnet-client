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
                        }
                    }
                }

            }
        };

        private readonly Dictionary<string, string> _manifest = new Dictionary<string, string>() { { "System.Web.Configuration", "system.web.configuration.recommendation.json" } };
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            //httpMessageHandler = new Mock<HttpMessageHandler>
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
    }
}
