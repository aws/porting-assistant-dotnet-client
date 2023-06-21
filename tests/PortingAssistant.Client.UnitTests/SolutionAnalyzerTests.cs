using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.CLI;
using NUnit.Framework.Internal.Execution;

namespace PortingAssistant.Client.UnitTests
{
    [TestFixture]
    public class SolutionAnalyzerTests
    {
        [Test]
        public void AnalyzeSolutionGenerator_ShouldThrowPortingAssistantException_WhenCancellationRequested()
        {
            // Arrange
            var portingAssistantClientMock = new Mock<IPortingAssistantClient>();
            var solutionPath = "path/to/solution.sln";
            var solutionSettings = new AnalyzerSettings();
            var cancellationToken = new CancellationToken(true);

            // Act and Assert
            Assert.ThrowsAsync<PortingAssistantException>(() =>
            Program.AnalyzeSolutionGenerator(
                    portingAssistantClientMock.Object,
                    solutionPath,
                    solutionSettings,
                    cancellationToken
                )
            );
        }

    }
}
