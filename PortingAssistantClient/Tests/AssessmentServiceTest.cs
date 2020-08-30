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

namespace EncoreAssesmentServiceTest
{
    public class AssessmentServiceTest
    {
        private Mock<IAssessmentHandler> _handler;
        private AssessmentService _assessmentService;
        private Response<ProjectAnalysisResult, SolutionProject> _getProjectResponse;
        private Response<PackageVersionResult, PackageVersionPair> _getNugetResponse;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _handler = new Mock<IAssessmentHandler>();
            _assessmentService = new AssessmentService(
                NullLogger<AssessmentService>.Instance,
                _handler.Object);
        }

        [SetUp]
        public void SetUp()
        {
            _assessmentService.AddApiAnalysisListener(response =>
            {
                _getProjectResponse = response;
            });

            _assessmentService.AddNugetPackageListener(response =>
            {
                _getNugetResponse = response;
            });

            _handler.Reset();

            _handler.Setup(handler => handler.GetSolutions(It.IsAny<List<string>>()))
                .Returns((List<string> list) =>
                {
                    return list.Distinct().Select(path =>
                    {
                        return new Solution
                        {
                            SolutionPath = path,
                            NumProjects = 10,
                        };
                    }).ToList();
                });

            _handler.Setup(handler => handler.GetProjects(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string pathToSolution, bool projectsOnly) =>
                {
                    var project = new Project
                    {
                        ProjectName = "testProject",
                        ProjectGuid = "guidxxxx",
                        ProjectPath = "/test/testProject",
                        ProjectType = "MSBUILD",
                        ProjectReferences = new List<ProjectReference>
                        {
                            new ProjectReference { ReferencePath = "/test/solution"
                        }},
                        NugetDependencies = new List<PackageVersionPair>
                        {
                            new PackageVersionPair
                            {
                                PackageId = "testpackage",
                                Version = "3.0.0"
                            }
                        },
                        TargetFrameworks = new List<string> { "netcoreapp3.1.0" }
                    };

                    var solutionAnalysisResult = new SolutionAnalysisResult
                    {
                        ProjectAnalysisResults = new Dictionary<string, Task<ProjectAnalysisResult>>
                        {
                            ["/test/testproject"] = getProjectAnalysisResult()
                        }
                    };

                    if (projectsOnly)
                    {
                        return new GetProjectResult
                        {
                            Projects = new List<Project>
                            {
                                project
                            }
                        };
                    }
                    else
                    {
                        return new GetProjectResult
                        {
                            Projects = new List<Project>
                            {
                                project
                            },
                            ApiInvocations = solutionAnalysisResult
                        };
                    }
                });

            _handler.Setup(handler => handler.GetNugetPackages(It.IsAny<List<PackageVersionPair>>(), It.IsAny<string>()))
                .Returns((List<PackageVersionPair> packageversions, string solutionPath) =>
                {
                    return packageversions.Distinct().Select(packageversion =>
                    {
                        return new Tuple<PackageVersionPair, Task<PackageVersionResult>>(packageversion, getPackageVersionResultAsync());

                    }).ToDictionary(t => t.Item1, t => t.Item2);
                });
        }

        private async Task<ProjectAnalysisResult> getProjectAnalysisResult()
        {
            await Task.Delay(5);

            return new ProjectAnalysisResult
            {
                ElapseTime = 1000,
                SourceFileToInvocations = null,
                SolutionFile = "/test/solution",
                ProjectFile = "/test/solution/testproject"
            };
        }

        private Task<ProjectAnalysisResult> getProjectAnalysisResultWithError()
        {
            var task = new TaskCompletionSource<ProjectAnalysisResult>();
            task.SetException(new Exception("failed"));
            return task.Task;
        }

        private async Task<PackageVersionResult> getPackageVersionResultAsync()
        {
            await Task.Delay(5);
            return new PackageVersionResult
            {
                PackageId = "testpackage",
                Version = "3.1.0",
                Compatible = Compatibility.COMPATIBLE,
                packageUpgradeStrategies = new List<string> { "3.2.0"}
            };
        }

        private async Task<PackageDetails> getPackageDetailAsync()
        {
            await Task.Delay(5);
            return new PackageDetails
            {
                Api = null,
                License = new LicenseDetails
                {
                    License = new Dictionary<string, SortedSet<string>>
                    {
                        ["MIT"] = new SortedSet<string> { "abcd" }
                    }
                },
                Name = "testpackage",
                Versions = new SortedSet<string> { "3.0.0" },
                Targets = new Dictionary<string, SortedSet<string>>
                {
                    ["netcoreapp3.1"] = new SortedSet<string> { "3.1.0" }
                }
            };
        }

        private Task<PackageVersionResult> getPackateDetailWithError()
        {
            var task = new TaskCompletionSource<PackageVersionResult>();
            task.SetException(new Exception("failed"));
            return task.Task;
        }


        [Test]
        public void TestGetSolutions()
        {
            var result = _assessmentService.GetSolutions(
                new GetSolutionsRequest
                {
                    SolutionPaths = new List<string>
                    {
                        "/test/solution"
                    }
                });
            Assert.AreEqual("/test/solution", result.Value.GetValueOrDefault("/test/solution").SolutionPath);
            Assert.AreEqual(10, result.Value.GetValueOrDefault("/test/solution").NumProjects);
            Assert.AreEqual(Response<Dictionary<string, Solution>, object>.ResponseStatus.StatusCode.Success, result.Status.Status);
        }

        [Test]
        public void TestGetSolutionsWitException()
        {
            _handler.Reset();

            _handler.Setup(handler => handler.GetSolutions(It.IsAny<List<string>>()))
                .Throws(new Exception("test error"));

            var result = _assessmentService.GetSolutions(
                new GetSolutionsRequest
                {
                    SolutionPaths = new List<string>
                    {
                        "/test/solution"
                    }
                });
            Assert.AreEqual(Response<Dictionary<string, Solution>, object>.ResponseStatus.StatusCode.Failure, result.Status.Status);
            Assert.AreEqual("test error", result.Status.Error.Message);
        }

        [Test]
        public async Task TestGetProjects()
        {
            var result = _assessmentService.GetProjects(
                new GetProjectsRequest
                {
                    ProjectsOnly = false,
                    SolutionPath = "/test/solution"
                });
            Assert.AreEqual("testProject", result.Value[0].ProjectName);
            Assert.AreEqual("guidxxxx", result.Value[0].ProjectGuid);
            Assert.AreEqual("/test/testProject", result.Value[0].ProjectPath);
            Assert.AreEqual("MSBUILD", result.Value[0].ProjectType);
            Assert.AreEqual("/test/solution", result.Value[0].ProjectReferences[0].ReferencePath);
            Assert.AreEqual(new PackageVersionPair
            {
                PackageId = "testpackage",
                Version = "3.0.0"
            }, result.Value[0].NugetDependencies[0]);
            Assert.AreEqual("netcoreapp3.1.0", result.Value[0].TargetFrameworks[0]);
            Assert.AreEqual(Response<List<Project>, List<string>>.ResponseStatus.StatusCode.Success, result.Status.Status);

            //ProjectAnalysResult
            await Task.Delay(10);
            Assert.AreEqual(1000, _getProjectResponse.Value.ElapseTime);
            Assert.Null(_getProjectResponse.Value.SourceFileToInvocations);
            Assert.AreEqual("/test/solution", _getProjectResponse.Value.SolutionFile);
            Assert.AreEqual("/test/solution/testproject", _getProjectResponse.Value.ProjectFile);
            Assert.AreEqual(Response<ProjectAnalysisResult, SolutionProject>.ResponseStatus.StatusCode.Success,
                _getProjectResponse.Status.Status);
            Assert.Null(_getProjectResponse.ErrorValue);
        }

        [Test]
        public async Task TestGetprojectsWithError()
        {
            _handler.Reset();

            _handler.Setup(handler => handler.GetProjects(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string pathToSolution, bool projectsOnly) =>
                {
                    var solutionAnalysisResult = new SolutionAnalysisResult
                    {
                        ProjectAnalysisResults = new Dictionary<string, Task<ProjectAnalysisResult>>
                        {
                            ["/test/failed"] = getProjectAnalysisResultWithError()
                        }
                    };

                    if (projectsOnly)
                    {
                        return new GetProjectResult
                        {
                            Projects = new List<Project>()
                        };
                    }
                    else
                    {
                        return new GetProjectResult
                        {
                            Projects = new List<Project>(),
                            ApiInvocations = solutionAnalysisResult
                        };
                    }
                });

            var result = _assessmentService.GetProjects(
                new GetProjectsRequest
                {
                    ProjectsOnly = false,
                    SolutionPath = "/test/solution"
                });

            await Task.Delay(10);
            Assert.AreEqual(null, _getProjectResponse.Value);
            Assert.AreEqual("/test/solution", _getProjectResponse.ErrorValue.SolutionPath);
            Assert.AreEqual("/test/failed", _getProjectResponse.ErrorValue.ProjectPath);
            Assert.AreEqual(Response<ProjectAnalysisResult, SolutionProject>.ResponseStatus.StatusCode.Failure,
                _getProjectResponse.Status.Status);
        }

        [Test]
        public void TestGetProjectsWitException()
        {
            _handler.Reset();

            _handler.Setup(handler => handler.GetProjects(It.IsAny<string>(), It.IsAny<bool>()))
                .Throws(new Exception("test error"));

            var result = _assessmentService.GetProjects(
                new GetProjectsRequest
                {
                    ProjectsOnly = false,
                    SolutionPath = "/test/solution"
                });
            Assert.AreEqual(Response<List<Project>, List<string>>.ResponseStatus.StatusCode.Failure, result.Status.Status);
            Assert.AreEqual("test error", result.Status.Error.Message);
        }

        [Test]
        public async Task TestGetNugetPackages()
        {
            _assessmentService.GetNugetPackages(
                new GetNugetPackagesRequest
                {
                    SolutionPath = "/test/solution",
                    PackageVersions = new List<PackageVersionPair>
                    {
                        new PackageVersionPair
                        {
                            PackageId = "testpackage",
                            Version = "3.0.0"
                        }
                    }
                });

            await Task.Delay(10);
            Assert.AreEqual("testpackage", _getNugetResponse.Value.PackageId);
            Assert.AreEqual("3.1.0", _getNugetResponse.Value.Version);
            Assert.AreEqual(Compatibility.COMPATIBLE, _getNugetResponse.Value.Compatible);
            Assert.True(_getNugetResponse.Value.packageUpgradeStrategies.Contains("3.2.0"));
            Assert.Null(_getNugetResponse.ErrorValue);
            Assert.AreEqual(Response<PackageVersionResult, PackageVersionPair>.ResponseStatus.StatusCode.Success,
                _getNugetResponse.Status.Status);
        }

        [Test]
        public async Task TestGetNugetPackagesWithError()
        {
            _handler.Reset();

            _handler.Setup(handler => handler.GetNugetPackages(It.IsAny<List<PackageVersionPair>>(), It.IsAny<string>()))
                .Returns((List<PackageVersionPair> packageversions, string solutionPath) =>
                {
                    return packageversions.Distinct().Select(packageversion =>
                    {
                        return new Tuple<PackageVersionPair, Task<PackageVersionResult>>(packageversion, getPackateDetailWithError());

                    }).ToDictionary(t => t.Item1, t => t.Item2);
                });

            _assessmentService.GetNugetPackages(
                new GetNugetPackagesRequest
                {
                    SolutionPath = "/test/solution",
                    PackageVersions = new List<PackageVersionPair>
                    {
                        new PackageVersionPair
                        {
                            PackageId = "testpackage",
                            Version = "3.0.0"
                        }
                    }
                });

            await Task.Delay(10);
            Assert.AreEqual(null, _getNugetResponse.Value);
            Assert.AreEqual(new PackageVersionPair
            {
                PackageId = "testpackage",
                Version = "3.0.0"
            }, _getNugetResponse.ErrorValue);
            Assert.AreEqual(Response<PackageVersionResult, PackageVersionPair>.ResponseStatus.StatusCode.Failure,
                _getNugetResponse.Status.Status);
        }

        [Test]
        public async Task TestGetNugetPackagesWithException()
        {
            _handler.Reset();

            _handler.Setup(handler => handler.GetNugetPackages(It.IsAny<List<PackageVersionPair>>(), It.IsAny<string>()))
                .Throws(new Exception("error"));

            var response = _assessmentService.GetNugetPackages(
                new GetNugetPackagesRequest
                {
                    SolutionPath = "/test/solution",
                    PackageVersions = new List<PackageVersionPair>
                    {
                        new PackageVersionPair
                        {
                            PackageId = "testpackage",
                            Version = "3.0.0"
                        }
                    }
                });

            await Task.Delay(10);
            Assert.AreEqual("/test/solution", response.ErrorValue);
            Assert.AreEqual(Response<string, string>.ResponseStatus.StatusCode.Failure, response.Status.Status);
        }
    }
}