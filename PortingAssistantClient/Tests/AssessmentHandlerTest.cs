using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EncoreApiAnalysis;
using EncoreAssessment;
using EncoreCache;
using EncoreCommon.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;


namespace EncoreAssessmentTest
{
    public class AssessmentHandlerTest
    {

        private Mock<IEncoreCacheHandler> _encoreCacheMock;
        private Mock<IEncoreApiAnalysisHandler> _apiAnalysisMock;
        private AssessmentHandler _assessmentHandler;
        private readonly string _solutionFolder = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithProjects");

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _encoreCacheMock = new Mock<IEncoreCacheHandler>();
            _apiAnalysisMock = new Mock<IEncoreApiAnalysisHandler>();
            _assessmentHandler = new AssessmentHandler(
                NullLogger<AssessmentHandler>.Instance,
                _encoreCacheMock.Object,
                _apiAnalysisMock.Object);
        }

        [SetUp]
        public void SetUp()
        {
            _encoreCacheMock.Reset();

            // Setup Nuget Dependencies
            _encoreCacheMock
                .Setup(cache => cache.GetNugetPackages(It.IsAny<List<PackageVersionPair>>(), It.IsAny<string>()))
                .Returns((List<PackageVersionPair> list, string pathToSolution) =>
                {
                    return list.Distinct().Select(packageVersion =>
                    {
                        var taskCompletionSource = new TaskCompletionSource<PackageVersionResult>();
                        taskCompletionSource.SetResult(
                            new PackageVersionResult
                            {
                                PackageId = packageVersion.PackageId,
                                Version = packageVersion.Version,
                                Compatible = Compatibility.COMPATIBLE,
                                packageUpgradeStrategies = new List<string> { packageVersion.Version}
                            });
                        return new Tuple<PackageVersionPair, Task<PackageVersionResult>>(
                            packageVersion, taskCompletionSource.Task
                        );
                    }).ToDictionary(t => t.Item1, t => t.Item2);
                });
        }

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
            Assert.Contains("EncoreApi", results.Projects.Select(result => result.ProjectName).ToList());

            var project = results.Projects.First(project => project.ProjectName.Equals("EncoreApi"));
            Assert.Contains("nunit", project.NugetDependencies.Select(dep => dep.PackageId).ToList());
            Assert.Contains(Path.Combine(_solutionFolder, "EncoreAssessment", "EncoreAssessment.csproj"),
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
        }


        [Test]
        public void TestGetNugetPackages()
        {
            var projects = _assessmentHandler.GetProjects(Path.Combine(_solutionFolder, "ModernSolution.sln"), false);
            var nugetDependencies = projects.Projects.SelectMany(p => p.NugetDependencies).ToList();
            var results = _assessmentHandler.GetNugetPackages(
                nugetDependencies,
                Path.Combine(_solutionFolder, "ModernSolution.sln"));

            var testdata = nugetDependencies.Distinct().ToList();

            Assert.AreEqual(nugetDependencies.Distinct().ToList().Count, results.ToList().Count);
            Assert.AreEqual(nugetDependencies.Distinct().Select(n => n.PackageId).ToHashSet(), results.Select(r => r.Key.PackageId).ToHashSet());
        }
    }
}