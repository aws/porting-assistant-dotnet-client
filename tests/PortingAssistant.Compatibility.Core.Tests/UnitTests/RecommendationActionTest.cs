using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PortingAssistant.Compatibility.Common.Interface;

namespace PortingAssistant.Compatibility.Core.Tests.UnitTests
{
    public class RecommendationActionTest
    {
        private Mock<IHttpService> _httpService;
        private ICompatibilityCheckerRecommendationActionHandler _compatibilityCheckerRecommendationActionHandler;
        private Mock<ILogger> _loggerMock;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _httpService = new Mock<IHttpService>();
        }

        [SetUp]
        public void Setup()
        {
            _compatibilityCheckerRecommendationActionHandler = new CompatibilityCheckerRecommendationActionHandler(
                _httpService.Object, Mock.Of<ILogger<CompatibilityCheckerRecommendationActionHandler>>());

            _loggerMock.Reset();
        }

        [Test]
        public async Task GetRecommendationAction_NamespaceNotFound_Return404Exception()
        {
            _httpService
                .Setup(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("404 not found"));

            IEnumerable<string> namepaces = new List<string>() { "test.namespace" };
            var resultTasks = await _compatibilityCheckerRecommendationActionHandler.GetRecommendationActionFileAsync( namepaces);

            Assert.AreEqual(1, resultTasks.Count);
            Assert.IsNull(resultTasks[namepaces.First()]);
            _loggerMock.Verify(mock => mock.LogInformation(It.IsAny<string>()), Times.Once);
            _loggerMock.Verify(mock => mock.LogError(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GetRecommendationAction_ReturnOtherException()
        {
            _httpService
                .Setup(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("error"));

            IEnumerable<string> namepaces = new List<string>() { "test.namespace" };
            var resultTasks = await _compatibilityCheckerRecommendationActionHandler.GetRecommendationActionFileAsync( namepaces);

            Assert.AreEqual(1, resultTasks.Count);
            Assert.IsNull(resultTasks[namepaces.First()]);
            _loggerMock.Verify(mock => mock.LogInformation(It.IsAny<string>()), Times.Never);
            _loggerMock.Verify(mock => mock.LogError(It.IsAny<string>()), Times.Once);
        }
    }
}
