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
            var telemetryClientConfig = new TelemetryClientConfig("someUrl");
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
