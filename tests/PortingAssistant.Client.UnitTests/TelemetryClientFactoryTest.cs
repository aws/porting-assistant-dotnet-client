using System;
using NUnit.Framework;
using PortingAssistant.Client.Telemetry;
using PortingAssistantExtensionTelemetry.Model;
using static System.Net.WebRequestMethods;

namespace PortingAssistant.Client.UnitTests
{
    public class TelemetryClientFactoryTest
    {
        [Test]
        public void EnablingDefaultCredentials_CreatesTelemetryClient()
        {
            var enabledDefaultCredentials = true;
            var telemetryConfig = new TelemetryConfiguration()
            {
                InvokeUrl = @"https://8cvsix1u33.execute-api.us-east-1.amazonaws.com/gamma",
                Region = "us-east-1",
            };
            var isClientCreated = TelemetryClientFactory.TryGetClient(
                "NonExistentProfile", 
                telemetryConfig, 
                out var client,
                enabledDefaultCredentials);

            Assert.IsTrue(isClientCreated);
        }
    }
}
