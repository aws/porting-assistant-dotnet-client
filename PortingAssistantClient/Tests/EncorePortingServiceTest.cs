using System;
using EncoreApiCommon.Services;
using System.Collections.Generic;
using EncoreCommon.Model;
using EncoreApiCommon.Model;
using EncoreAssessment.Model;
using System.Linq;
using EncoreAssessment;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using EncorePorting;

namespace EncorePortingTest
{
    public class EncorePortingServiceTest
    {
        private IPortingService _portService;
        private Mock<IPortingHandler> _handler;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _handler = new Mock<IPortingHandler>();
            _portService = new PortingService(
                NullLogger<PortingService>.Instance,
                _handler.Object
                );
        }

        [SetUp]
        public void SetUp()
        {
            _handler.Reset();

            _handler.Setup(handler => handler.ApplyPortProjectFileChanges(
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>()))
                .Returns(
                (List<string> projectPaths,
                string solutionPath,
                string targetFramework,
                Dictionary<string, string> upgradeVersions) =>
                {
                    return projectPaths.Distinct().Select(projectPath =>
                    {
                        if(projectPath == "/test/failed")
                        {
                            return new PortingProjectFileResult
                            {
                                Success = false,
                                ProjectFile = projectPath,
                                ProjectName = "testfailedproject",
                                Message = "error",
                                Execption = new Exception("error")
                            };
                        }
                        return new PortingProjectFileResult {
                            Success = true,
                            ProjectFile = projectPath,
                            ProjectName = "testproject" 
                        };

                    }).ToList();
                });
        }

        [Test]
        public void PortServiceTest()
        {
            var result = _portService.ApplyPortingProjectFileChanges(
                new ApplyPortingProjectFileChangesRequest
                {
                    ProjectPaths = new List<string>
                    {
                        "/test/solution/testproject",
                        "/test/failed"
                    },
                    SolutionPath = "/test/solution",
                    TargetFramework = "netcoreapp3.1.0",
                    UpgradeVersions = new Dictionary<string, string>
                    {
                        ["Newton.Json"] = "12.0.3"
                    }
                });

            Assert.AreEqual(Response<List<PortingProjectFileResult>, List<PortingProjectFileResult>>
                .ResponseStatus.StatusCode.Success, result.Status.Status);
            Assert.AreEqual("/test/solution/testproject",
                result.Value.Find(package => package.ProjectName == "testproject").ProjectFile);
            Assert.AreEqual("/test/failed",
                result.ErrorValue.Find(package => package.ProjectName == "testfailedproject").ProjectFile);
        }

        [Test]
        public void PortServiceWithException()
        {
            _handler.Reset();
            _handler.Setup(handler => handler.ApplyPortProjectFileChanges(
                 It.IsAny<List<string>>(),
                 It.IsAny<string>(),
                 It.IsAny<string>(),
                 It.IsAny<Dictionary<string, string>>()))
                .Throws(new Exception("test error"));

            var result = _portService.ApplyPortingProjectFileChanges(
                new ApplyPortingProjectFileChangesRequest
                {
                    ProjectPaths = new List<string>
                    {
                        "/test/solution/testproject",
                        "/test/failed"
                    },
                    SolutionPath = "/test/solution",
                    TargetFramework = "netcoreapp3.1.0",
                    UpgradeVersions = new Dictionary<string, string>
                    {
                        ["Newton.Json"] = "12.0.3"
                    }
                });

            Assert.AreEqual(Response<List<PortingProjectFileResult>, List<PortingProjectFileResult>>
                .ResponseStatus.StatusCode.Failure, result.Status.Status);
            Assert.AreEqual("test error", result.Status.Error.Message);

        }
    }
}
