using System;
using System.Collections.Generic;
using EncoreApiAnalysis.Model;
using EncoreCommon.Model;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Tests
{
    public class EncoreApiCompatibilityResponseTest
    {
        [Test]
        public void TestDeserialize()
        {
            var json = "[{\"methodSignature\": \"Signature1\", \"compatible\": \"NOT_FOUND\"}, {\"methodSignature\": \"Signature2\", \"compatible\": \"INCOMPATIBLE\"}, {\"methodSignature\": \"Signature3\", \"compatible\": \"COMPATIBLE\"}]";
            var result = JsonConvert.DeserializeObject<List<EncoreApiCompatibilityResponse>>(json);
            Assert.AreEqual(new List<EncoreApiCompatibilityResponse> {
                new EncoreApiCompatibilityResponse
                {
                    MethodSignature = "Signature1",
                    Compatible = Compatibility.NOT_FOUND
                },
                new EncoreApiCompatibilityResponse
                {
                    MethodSignature = "Signature2",
                    Compatible = Compatibility.INCOMPATIBLE
                },
                new EncoreApiCompatibilityResponse
                {
                    MethodSignature = "Signature3",
                    Compatible = Compatibility.COMPATIBLE
                }
            }, result);
        }

        [Test]
        public void TestInvalidJson()
        {
            var json = "[{\"methodSignature\": \"Signature1\", \"result\": \"NOT_FOUND\"},]";
            var result = JsonConvert.DeserializeObject<List<EncoreApiCompatibilityResponse>>(json);
            Assert.AreNotEqual(new List<EncoreApiCompatibilityResponse> {
                new EncoreApiCompatibilityResponse
                {
                    MethodSignature = "Signature1",
                    Compatible = Compatibility.COMPATIBLE
                }
            }, result);
            Assert.AreNotEqual(result[0].Compatible, Compatibility.COMPATIBLE);
        }
    }
}
