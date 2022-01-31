using System.Collections.Generic;
using NuGet.Versioning;
using NUnit.Framework;
using PortingAssistant.Client.Analysis.Utils;

namespace PortingAssistant.Client.UnitTests
{
    public class NugetVersionHelperTest
    {
        [Test]
        public void GetMaxVersion_Returns_Largest_Version()
        {
            var expectedResult = NuGetVersion.Parse("3.0.0");
            var actualResult = NugetVersionHelper.GetMaxVersion(new List<string>
            {
                "1.0.0",
                "2.1.0",
                "3.0.0",
                "2.9.0"
            });

            Assert.AreEqual(expectedResult.ToString(), actualResult.ToString());
        }

        [Test]
        public void GetMaxVersion_Returns_Null_When_Input_Is_Empty()
        {
            var actualResult = NugetVersionHelper.GetMaxVersion(new List<string>());
            Assert.IsNull(actualResult);
        }

        [Test]
        public void HasLowerCompatibleVersionWithSameMajor_Returns_True_With_LowerCompatibleVersionWithSameMajor()
        {
            var nugetVersion = NuGetVersion.Parse("3.1.0");
            var actualResult = NugetVersionHelper.HasLowerCompatibleVersionWithSameMajor(nugetVersion, new List<string>
            {
                "3.0.0"
            });
            Assert.IsTrue(actualResult);
        }

        [Test]
        public void HasLowerCompatibleVersionWithSameMajor_Returns_False_Without_LowerCompatibleVersionWithSameMajor()
        {
            var nugetVersion = NuGetVersion.Parse("3.1.0");
            var actualResult = NugetVersionHelper.HasLowerCompatibleVersionWithSameMajor(nugetVersion, new List<string>
            {
                "2.9.0",
                "3.2.0"
            });
            Assert.IsFalse(actualResult);
        }
    }
}