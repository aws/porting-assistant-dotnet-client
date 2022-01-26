using NuGet.Versioning;
using NUnit.Framework;
using PortingAssistant.Client.Analysis.Utils;
using System.Collections.Generic;

namespace PortingAssistant.Client.UnitTests
{
    public class NugetVersionExtensionTest
    {
        private NuGetVersion _nugetVersion;
        private const string VersionToTest = "2.1.1";

        private const string EqualVersion = "2.1.1";
        private const string GreaterVersion = "2.1.2";
        private const string LowerVersion = "2.1.0";

        private const string GreaterMajorVersion = "3.1.1";
        private const string LowerMajorVersion = "1.1.1";

        private const string ZeroVersion1 = "0.0.0";
        private const string ZeroVersion2 = "0.0.0.0";

        [SetUp]
        public void Setup()
        {
            _nugetVersion = NuGetVersion.Parse(VersionToTest);
        }

        [TestCase(EqualVersion, true)]
        [TestCase(GreaterVersion, false)]
        [TestCase(LowerVersion, true)]
        public void IsGreaterThanOrEqualTo_Returns_Correct_Value(string versionToCompare, bool expectedResult)
        {
            var actualResult = _nugetVersion.IsGreaterThanOrEqualTo(versionToCompare);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestCase(ZeroVersion1, true)]
        [TestCase(ZeroVersion2, true)]
        [TestCase(VersionToTest, false)]
        public void IsZeroVersion_Returns_Correct_Value(string version, bool expectedResult)
        {
            var nugetVersion = NuGetVersion.Parse(version);
            var actualResult = nugetVersion.IsZeroVersion();
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestCase(EqualVersion, false)]
        [TestCase(GreaterVersion, false)]
        [TestCase(LowerVersion, true)]
        public void IsGreaterThan_Returns_Correct_Value(string versionToCompare, bool expectedResult)
        {
            var actualResult = _nugetVersion.IsGreaterThan(versionToCompare);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestCase(EqualVersion, true)]
        [TestCase(GreaterVersion, true)]
        [TestCase(LowerVersion, false)]
        public void IsLessThanOrEqualTo_Returns_Correct_Value(string versionToCompare, bool expectedResult)
        {
            var actualResult = _nugetVersion.IsLessThanOrEqualTo(versionToCompare);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestCase(GreaterMajorVersion, false)]
        [TestCase(LowerMajorVersion, false)]
        [TestCase(GreaterVersion, true)]
        [TestCase(LowerVersion, true)]
        public void HasSameMajorAs_Returns_Correct_Value(string versionToCompare, bool expectedResult)
        {
            var actualResult = _nugetVersion.HasSameMajorAs(versionToCompare);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [Test]
        public void FindGreaterCompatibleVersions_Returns_Greater_Versions()
        {
            var compatibleVersions = new List<string>
            {
                "1.9.0",
                "2.1.0",
                "2.1.1", 
                "2.1.2", 
                "2.1.4", 
                "2.4.1"
            };
            var expectedResult = new List<string> 
            {  
                "2.1.2",
                "2.1.4",
                "2.4.1"
            };

            var actualResult = _nugetVersion.FindGreaterCompatibleVersions(compatibleVersions);
            CollectionAssert.AreEquivalent(expectedResult, actualResult);
        }
    }
}
