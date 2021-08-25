using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using PortingAssistant.Client.Client.Utils;

namespace PortingAssistant.Client.UnitTests
{
    public class HashUtilsTest
    {
        [Test]
        public void GenerateGuid_Returns_Expected_Hash()
        {
            List<string> guids = new List<string> {"a", "b", "c" };
            string actualHash = HashUtils.GenerateGuid(guids);
            string expectedHash = "b16bfbd6-a33f-e8a2-3f5f-f8477184d858";
            Assert.AreEqual(expectedHash, actualHash);
        }

        [Test]
        public void GenerateGuid_Returns_Expected_On_Empty_Guid_Values()
        {
            List<string> guids = new List<string> { "", "", "" };
            string actualHash = HashUtils.GenerateGuid(guids);
            string expectedHash = "1b01c3c3-99e5-cc66-54e9-cdac516c7d62";
            Assert.AreEqual(expectedHash, actualHash);
        }

        [Test]
        public void GenerateGuid_Returns_Expected_On_Null_Guid_Values()
        {
            List<string> guids = new List<string> { null, null, null };
            string actualHash = HashUtils.GenerateGuid(guids);
            string expectedHash = "1b01c3c3-99e5-cc66-54e9-cdac516c7d62";
            Assert.AreEqual(expectedHash, actualHash);
        }

        [Test]
        public void GenerateGuid_Returns_Null_On_Empty_Input()
        {
            List<string> guids = new List<string> { };
            string actualHash = HashUtils.GenerateGuid(guids);
            Assert.AreEqual(null, actualHash);
        }

        [Test]
        public void GenerateGuid_Returns_Null_On_Null_Input()
        {
            string actualHash = HashUtils.GenerateGuid(null);
            Assert.AreEqual(null, actualHash);
        }
    }
}
