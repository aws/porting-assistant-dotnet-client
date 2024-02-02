using System;
using Amazon.Runtime;
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
                InvokeUrl = @"https://dummy.amazonaws.com/gamma",
                Region = "us-east-1",
            };
            var isClientCreated = TelemetryClientFactory.TryGetClient(
                "NonExistentProfile", 
                telemetryConfig, 
                out var client,
                enabledDefaultCredentials);

            Assert.IsTrue(isClientCreated);
        }

        [Test]
        public void CreatesTelemetryClient_WithCredentials()
        {
            string profile = null;
            var telemetryConfig = new TelemetryConfiguration()
            {
                InvokeUrl = @"https://dummy.amazonaws.com/gamma",
                Region = "us-east-1",
            };
            AWSCredentials credentials = new BasicAWSCredentials("accessKey", "secretKey");
            ITelemetryClient client;

            bool result = TelemetryClientFactory.TryGetClient(profile, telemetryConfig, out client, awsCredentials: credentials);

            Assert.IsTrue(result);
            Assert.IsNotNull(client);
        }

        [Test]
        public void CreatesTelemetryClient_WithoutProfileOrCredentials_Failed()
        {
            string profile = null;
            var telemetryConfig = new TelemetryConfiguration()
            {
                InvokeUrl = @"https://dummy.amazonaws.com/gamma",
                Region = "us-east-1",
            };
            AWSCredentials credentials = null;
            ITelemetryClient client;

            bool result = TelemetryClientFactory.TryGetClient(profile, telemetryConfig, out client, awsCredentials: credentials);

            Assert.IsFalse(result);
            Assert.IsNull(client);
        }
    }
}
