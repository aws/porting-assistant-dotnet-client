using System;
using System.Collections.Generic;
using PortingAssistant.Model;
using System.Linq;
using PortingAssistant.NuGet;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Build.Construction;
using PortingAssistantHandler.FileParser;
using NuGet.Frameworks;
using NuGet.Versioning;
using PortingAssistant.ApiAnalysis;

namespace PortingAssistantApiAnalysisTest
{
    public class PortingAssistantApiAnalysisHandlerTest
    {
        private Mock<IPortingAssistantNuGetHandler> _handler;
        private PortingAssistantApiAnalysisHandler _PortingAssistantApiAnalysisHandler;
        private string solutionFile;
        private List<Project> projects;

        private readonly PackageDetails _packageDetails = new PackageDetails
        {
            Name = "Newtonsoft.Json",
            Versions = new SortedSet<string> { "12.0.3", "12.0.4" },
            Api = new ApiDetails[]
            {
                new ApiDetails
                {
                    MethodName = "Setup(Object)",
                    MethodSignature = "Newtonsoft.Json.JsonConvert.SerializeObject(object)",
                    Targets = new Dictionary<string, SortedSet<string>>
                    {
                        {
                             "netcoreapp3.1", new SortedSet<string> { "10.2.0","12.0.3", "12.0.4" }
                        }
                    },
                }
            },
            Targets = new Dictionary<string, SortedSet<string>> {
                {
                    "netcoreapp3.1",
                    new SortedSet<string> { "12.0.3", "12.0.4" }
                }
            },
            License = new LicenseDetails
            {
                License = new Dictionary<string, SortedSet<string>>
                {
                    { "MIT", new SortedSet<string> { "12.0.3", "12.0.4" } }
                }
            }
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _handler = new Mock<IPortingAssistantNuGetHandler>();
            _PortingAssistantApiAnalysisHandler = new PortingAssistantApiAnalysisHandler(NullLogger<PortingAssistantApiAnalysisHandler>.Instance, _handler.Object);
        }

        [SetUp]
        public void SetUp()
        {
            solutionFile = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithApi", "TestSolution.sln");
            projects = getProjects(solutionFile);

            _handler.Reset();
            _handler.Setup(handler => handler.GetPackageDetails(It.IsAny<PackageVersionPair>()))
                .Returns((PackageVersionPair package) =>
                {
                    var task = new TaskCompletionSource<PackageDetails>();
                    task.SetResult(_packageDetails);
                    return task.Task;
                });
        }

        private List<Project> getProjects(string pathToSolution)
        {
            var solution = SolutionFile.Parse(pathToSolution);
            var failedProjects = new List<string>();

            var projects = solution.ProjectsInOrder.Select(p =>
            {
                if (p.ProjectType != SolutionProjectType.KnownToBeMSBuildFormat && p.ProjectType != SolutionProjectType.WebProject)
                {
                    return null;
                }

                var projectParser = new ProjectFileParser(p.AbsolutePath);

                return new Project
                {
                    ProjectName = p.ProjectName,
                    ProjectPath = p.AbsolutePath,
                    ProjectGuid = p.ProjectGuid,
                    TargetFrameworks = projectParser.GetTargetFrameworks().Select(tfm =>
                    {
                        var framework = NuGetFramework.Parse(tfm);
                        return string.Format("{0} {1}", framework.Framework, NuGetVersion.Parse(framework.Version.ToString()).ToNormalizedString());
                    }).ToList(),
                    NugetDependencies = projectParser.GetPackageReferences(),
                    ProjectReferences = projectParser.GetProjectReferences()
                };
            }).Where(p => p != null).ToList();

            return projects;
        }

        [Test]
        public void AnalyzeSolution()
        {
            var result = _PortingAssistantApiAnalysisHandler.AnalyzeSolution(solutionFile, projects);
            Task.WaitAll(result.ProjectAnalysisResults.Values.ToArray());
            var values = result.ProjectAnalysisResults.Values.First().Result;
            Assert.AreEqual(projects.First().ProjectPath, values.ProjectFile);
            Assert.AreEqual("Newtonsoft.Json", values.SourceFileToInvocations.First().Value.First().invocation.PackageId);
            Assert.AreEqual("11.0.1", values.SourceFileToInvocations.First().Value.First().invocation.Version);
            Assert.AreEqual("Newtonsoft.Json.JsonConvert.SerializeObject(object)",
                values.SourceFileToInvocations.First().Value.First().invocation.OriginalDefinition);
            Assert.AreEqual(false, values.SourceFileToInvocations.First().Value.First().deprecated);
            Assert.AreEqual(true, values.SourceFileToInvocations.First().Value.First().isCompatible);
            Assert.AreEqual("12.0.4", values.SourceFileToInvocations.First().Value.First().replacement);
        }

        [Test]
        public void AnalyzeFaultPath()
        {
            Assert.Throws<AggregateException>(() =>
            {
                var result = _PortingAssistantApiAnalysisHandler.AnalyzeSolution("", projects);
                Task.WaitAll(result.ProjectAnalysisResults.Values.ToArray());
            });

            Assert.Throws<AggregateException>(() =>
            {
                var result = _PortingAssistantApiAnalysisHandler.AnalyzeSolution(Path.Combine(solutionFile, "radn.sln"), projects);
                Task.WaitAll(result.ProjectAnalysisResults.Values.ToArray());
            });
        }

    }
}
