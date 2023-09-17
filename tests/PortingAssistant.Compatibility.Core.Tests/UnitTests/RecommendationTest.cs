using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using PortingAssistant.Compatibility.Common.Utils;
using Microsoft.Extensions.Logging;
using PortingAssistant.Compatibility.Core.Checkers;

namespace PortingAssistant.Compatibility.Core.Tests.UnitTests
{
    public class RecommendationTest
    {
        private Mock<IHttpService> _httpService;
        private ICompatibilityCheckerRecommendationHandler _compatibilityCheckerRecommendationHandler;

        private readonly RecommendationDetails _recommendationDetails = new RecommendationDetails
        {
            Name = "System.Web.Configuration",
            Version = "1.0.0",
            Packages = new Packages[]
            {
                new Packages()
                {
                    Type = "SDK",
                    Name = "System.Web.Configuration"
                }
                
            },
            Recommendations = new RecommendationModel[]
            {
                new RecommendationModel
                {
                    Type = "Method",
                    Value = "System.Web.Configuration.BrowserCapabilitiesFactory.OperaminiProcessBrowsers(bool, System.Collections.Specialized.NameValueCollection, System.Web.HttpBrowserCapabilities)",
                    Name = "",
                    KeyType = "",
                    RecommendedActions = new RecommendedActionModel[]
                    {
                        new RecommendedActionModel()
                        {
                            Source = "Amazon",
                            Preferred = "yes",
                            TargetFrameworks = new SortedSet<string>()
                            {
                                "netframework4.5",
                                "netcoreapp3.1"
                            },
                            Description = "System.Web (AKA classic ASP.NET) won't be ported to .NET Core. See https://aka.ms/unsupported-netfx-api.",
                            
                        },
                        new RecommendedActionModel()
                        {
                            Source = "Amazon",
                            Preferred = "yes",
                            TargetFrameworks = new SortedSet<string>()
                            {
                                "net5.0"
                            },
                            Description = "No Recommendation in net5.0 now",
                        }
                    }
                }

            }
        };

        private readonly CompatibilityResult compatibilityResult = new CompatibilityResult
        {
            Compatibility = Common.Model.Compatibility.INCOMPATIBLE,
            CompatibleVersions = new List<string>()
        };

        private readonly IEnumerable<string> _namespaces = new List<string>() { "System.Web.Configuration" };

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _httpService = new Mock<IHttpService>();
        }

        [SetUp]
        public void Setup()
        { 
            _compatibilityCheckerRecommendationHandler = new CompatibilityCheckerRecommendationHandler(
                _httpService.Object, Mock.Of<ILogger<CompatibilityCheckerRecommendationHandler>>());
        }

        

        [Test]
        public void TestPackageCompatibilityResult()
        {
            var versions = new SortedSet<string>
            {
                "1.0.0",
                "1.0.1-beta",
                "2.0.0",
            };
            var packageVersionPair = new PackageVersionPair
            {
                PackageId = "MyNugetPackage",
                PackageSourceType = PackageSourceType.NUGET,
                Version = "1.0.0"
            };
            var packageDetails = new PackageDetails
            {
                Name = "MyNugetPackage",
                Versions = versions,
                Targets = new Dictionary<string, SortedSet<string>>
                {
                    { "netcoreapp3.1",  versions},
                    { "net6.0",  versions}
                }
            };

            var compatResults = PackageCompatibility.IsCompatibleAsync(Task.FromResult(packageDetails), packageVersionPair, Mock.Of<ILogger>());
            //var actions = new List<ActionFileActions>();
            var recommendation = PackageCompatibility.GetPackageAnalysisResult(compatResults.Result, packageVersionPair, "netcoreapp3.1", assessmentType: AssessmentType.FullAssessment);

            Assert.AreEqual(2, compatResults.Result.CompatibleVersions.Count);
            Assert.AreEqual(1, recommendation.CompatibilityResults["netcoreapp3.1"].CompatibleVersions.Count);
            //Assert.AreEqual("2.0.0", recommendation.Recommendations.RecommendedActions[0].Description);
            // No recommandation attached
            Assert.AreEqual(1, recommendation.Recommendations.RecommendedActions.Count);
        }

        [Test]
        public void TestIsCompatibleAsync_ReturnsIncompatible_PackageVersionNotInTargetVersions()
        {
            var versions = new SortedSet<string>
            {
                "4.0.0",
                "4.5.0"
            };
            var packageVersionPair = new PackageVersionPair
            {
                PackageId = "System.DirectoryServices",
                PackageSourceType = PackageSourceType.NUGET,
                Version = "5.0.0"
            };
            var packageDetails = new PackageDetails
            {
                Name = "System.DirectoryServices",
                Versions = versions,
                Targets = new Dictionary<string, SortedSet<string>>
                {
                    { "net5.0",  versions }
                }
            };

            var compatResults = PackageCompatibility.IsCompatibleAsync(Task.FromResult(packageDetails), packageVersionPair, Mock.Of<ILogger>());

            Assert.AreEqual(0, compatResults.Result.CompatibleVersions.Count);
            Assert.AreEqual(Common.Model.Compatibility.INCOMPATIBLE, compatibilityResult.Compatibility);
        }
        
    }

}
