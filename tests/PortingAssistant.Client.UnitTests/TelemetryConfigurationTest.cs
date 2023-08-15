using System.Collections.Generic;
using NUnit.Framework;
using PortingAssistantExtensionTelemetry.Model;

namespace PortingAssistant.Client.UnitTests
{
    public class TelemetryConfigurationTest
    {
        private TelemetryConfiguration telemetryConfiguration;
        [SetUp]
        public void Setup()
        {
            telemetryConfiguration = new TelemetryConfiguration();
        }

        [Test]
        public void TestSettersAndGetters()
        {
            telemetryConfiguration.InvokeUrl = nameof(telemetryConfiguration.InvokeUrl);
            telemetryConfiguration.Region = nameof(telemetryConfiguration.Region);
            telemetryConfiguration.LogsPath = nameof(telemetryConfiguration.LogsPath);
            telemetryConfiguration.ServiceName = nameof(telemetryConfiguration.ServiceName);
            telemetryConfiguration.Description = nameof(telemetryConfiguration.Description);
            telemetryConfiguration.LogFilePath = nameof(telemetryConfiguration.LogFilePath);
            telemetryConfiguration.MetricsFilePath = nameof(telemetryConfiguration.MetricsFilePath);
            telemetryConfiguration.LogPrefix = nameof(telemetryConfiguration.LogPrefix);
            telemetryConfiguration.LogsFolderSizeLimit = 10000;
            telemetryConfiguration.Suffix = new List<string> { nameof(telemetryConfiguration.Suffix) };

            Assert.AreEqual(nameof(telemetryConfiguration.InvokeUrl), telemetryConfiguration.InvokeUrl);
            Assert.AreEqual(nameof(telemetryConfiguration.Region), telemetryConfiguration.Region);
            Assert.AreEqual(nameof(telemetryConfiguration.LogsPath), telemetryConfiguration.LogsPath);
            Assert.AreEqual(nameof(telemetryConfiguration.ServiceName), telemetryConfiguration.ServiceName);
            Assert.AreEqual(nameof(telemetryConfiguration.Description), telemetryConfiguration.Description);
            Assert.AreEqual(nameof(telemetryConfiguration.LogFilePath), telemetryConfiguration.LogFilePath);
            Assert.AreEqual(nameof(telemetryConfiguration.MetricsFilePath), telemetryConfiguration.MetricsFilePath);
            Assert.AreEqual(nameof(telemetryConfiguration.LogPrefix), telemetryConfiguration.LogPrefix);
            Assert.AreEqual(10000, telemetryConfiguration.LogsFolderSizeLimit);
            CollectionAssert.AreEquivalent(new List<string> { nameof(telemetryConfiguration.Suffix) }, telemetryConfiguration.Suffix);
        }
    }
}
