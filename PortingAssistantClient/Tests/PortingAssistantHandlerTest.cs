using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PortingAssistant.ApiAnalysis;
using PortingAssistantHandler;
using PortingAssistant.NuGet;
using PortingAssistant.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;


namespace PortingAssistantAssessmentTest
{
    public class AssessmentHandlerTest
    {

        private Mock<IPortingAssistantNuGetHandler> _PortingAssistantNuGetMock;
        private Mock<IPortingAssistantApiAnalysisHandler> _apiAnalysisMock;
        private AssessmentHandler _assessmentHandler;
        private readonly string _solutionFolder = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithProjects");

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _PortingAssistantNuGetMock = new Mock<IPortingAssistantNuGetHandler>();
            _apiAnalysisMock = new Mock<IPortingAssistantApiAnalysisHandler>();
            _assessmentHandler = new AssessmentHandler(
                NullLogger<AssessmentHandler>.Instance,
                _PortingAssistantNuGetMock.Object,
                _apiAnalysisMock.Object);
        }

        [SetUp]
        public void SetUp()
        {
            _PortingAssistantNuGetMock.Reset();

            // Setup Nuget Dependencies
            _PortingAssistantNuGetMock
                .Setup(NuGet => NuGet.GetNugetPackages(It.IsAny<List<PackageVersionPair>>(), It.IsAny<string>()))
                .Returns((List<PackageVersionPair> list, string pathToSolution) =>
                {
                    return list.Distinct().Select(packageVersion =>
                    {
                        var taskCompletionSource = new TaskCompletionSource<PackageAnalysisResult>();
                        taskCompletionSource.SetResult(
                            new PackageAnalysisResult
                            {
                                PackageVersionPair = packageVersion,
                                PackageRecommendation = new PackageRecommendation
                                {
                                    RecommendedActionType = RecommendedActionType.UpgradePackage,
                                    TargetFrameworkCompatibleVersionPair = new Dictionary<string, PackageCompatibilityInfo>
                                    {
                                        {
                                            "netcoreapp3.1", new PackageCompatibilityInfo {
                                                CompatibilityResult = Compatibility.COMPATIBLE,
                                                CompatibleVersion = new List<string> { packageVersion.Version }
                                             }
                                        }
                                    }
                                }
                            });
                        return new Tuple<PackageVersionPair, Task<PackageAnalysisResult>>(
                            packageVersion, taskCompletionSource.Task
                        );
                    }).ToDictionary(t => t.Item1, t => t.Item2);
                });
        }
        /*
        [Test]
        public void TestGetSolution()
        {
            var solutions = _assessmentHandler.GetSolutions(new List<string>() {
                Path.Combine(_solutionFolder, "ModernSolution.sln")
            });
            Assert.AreEqual(1, solutions.Count);

            Assert.AreEqual(5, solutions.First().NumProjects);
        }

        [Test]
        public void TestGetSolutionWithExpection()
        {
            var solutions = _assessmentHandler.GetSolutions(new List<string>() {
                Path.Combine(_solutionFolder, "failed.sln")
            });
            Assert.AreEqual(0, solutions.Count);
        }

        [Test]
        public void TestGetProject()
        {
            var results = _assessmentHandler.GetProjects(Path.Combine(_solutionFolder, "ModernSolution.sln"), false);
            Assert.AreEqual(5, results.Projects.Count);
            Assert.Contains("PortingAssistantApi", results.Projects.Select(result => result.ProjectName).ToList());

            var project = results.Projects.First(project => project.ProjectName.Equals("PortingAssistantApi"));
            Assert.Contains("nunit", project.NugetDependencies.Select(dep => dep.PackageId).ToList());
            Assert.Contains(Path.Combine(_solutionFolder, "PortingAssistantAssessment", "PortingAssistantAssessment.csproj"),
                project.ProjectReferences.Select(proj => proj.ReferencePath).ToList());

            project = results.Projects.First(project => project.ProjectName.Equals("Nop.Core"));
            Assert.Contains("Autofac", project.NugetDependencies.Select(dep => dep.PackageId).ToList());
            Assert.Contains(".NETFramework 4.5.1", project.TargetFrameworks);
        }

        [Test]
        public void TestGetProjectWithCorruptedSlnFile()
        {
            var testSolutionPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithFailedContent", "NopCommerce.sln");

            var results = _assessmentHandler.GetProjects(testSolutionPath, true);
            Assert.AreEqual(0, results.Projects.Count);
            Assert.AreEqual(1, results.FailedProjects.Count);
        }*/
    }
}