using System.Collections.Generic;
using PortingAssistant.ApiAnalysis.Model;
using PortingAssistant.Model;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Tests
{
    public class PortingAssistantApiCompatibilityResponseTest
    {
        [Test]
        public void DeserializeValidJsonSucceeds()
        {
            var json = "[{\"methodSignature\": \"Signature1\", \"compatible\": \"UNKNOWN\"}, {\"methodSignature\": \"Signature2\", \"compatible\": \"INCOMPATIBLE\"}, {\"methodSignature\": \"Signature3\", \"compatible\": \"COMPATIBLE\"}]";
            var result = JsonConvert.DeserializeObject<List<PortingAssistantApiCompatibilityResponse>>(json);

            Assert.AreEqual(new List<PortingAssistantApiCompatibilityResponse> {
                new PortingAssistantApiCompatibilityResponse
                {
                    MethodSignature = "Signature1",
                    Compatible = Compatibility.UNKNOWN
                },
                new PortingAssistantApiCompatibilityResponse
                {
                    MethodSignature = "Signature2",
                    Compatible = Compatibility.INCOMPATIBLE
                },
                new PortingAssistantApiCompatibilityResponse
                {
                    MethodSignature = "Signature3",
                    Compatible = Compatibility.COMPATIBLE
                }
            }, result);
        }

        [Test]
        public void DeserializeInvalidJsonMapsToUnknownCompatibility()
        {
            var json = "[{\"methodSignature\": \"Signature1\", \"result\": \"NOT_FOUND\"},]";
            var result = JsonConvert.DeserializeObject<List<PortingAssistantApiCompatibilityResponse>>(json);

            Assert.AreNotEqual(new List<PortingAssistantApiCompatibilityResponse> {
                new PortingAssistantApiCompatibilityResponse
                {
                    MethodSignature = "Signature1",
                    Compatible = Compatibility.COMPATIBLE
                }
            }, result);
            Assert.AreNotEqual(result[0].Compatible, Compatibility.COMPATIBLE);
            Assert.AreEqual(result[0].Compatible, Compatibility.UNKNOWN);
        }
    }
}
