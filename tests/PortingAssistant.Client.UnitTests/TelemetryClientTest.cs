using Amazon.Runtime;
using NUnit.Framework;
using PortingAssistant.Client.Telemetry;

namespace PortingAssistant.Client.UnitTests
{
    public class TelemetryClientTest
    {
        [Test]
        public void EnablingDefaultCredentials_CreatesTelemetryClient()
        {
            var url = "https://8cvsix1u33.execute-api.us-east-1.amazonaws.com/gamma";
            var telemetryClientConfig = new TelemetryClientConfig(url)
            {
                ServiceURL = url
            };

            var fallbackCredentials = FallbackCredentialsFactory.GetCredentials();
            TelemetryClient client;

            client = new TelemetryClient(telemetryClientConfig);
            Assert.IsNotNull(client);

            client = new TelemetryClient(fallbackCredentials, telemetryClientConfig);
            Assert.IsNotNull(client);

            client = new TelemetryClient(
                "AccessKey",
                "SecretKey",
                telemetryClientConfig);
            Assert.IsNotNull(client);
            
            client = new TelemetryClient(
                "AccessKey",
                "SecretKey",
                "token",
                telemetryClientConfig);
            Assert.IsNotNull(client);
        }
    }
}
