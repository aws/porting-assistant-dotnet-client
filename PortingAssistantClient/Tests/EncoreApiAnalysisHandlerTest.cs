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
        private Mock<IPortingAssistantNamespaceHandler> _nameSpacehandler;
        private PortingAssistantApiAnalysisHandler _PortingAssistantApiAnalysisHandler;
        private string solutionFile;
        private List<ProjectDetails> projects;

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
            _nameSpacehandler = new Mock<IPortingAssistantNamespaceHandler>();
            _PortingAssistantApiAnalysisHandler = new PortingAssistantApiAnalysisHandler(NullLogger<PortingAssistantApiAnalysisHandler>.Instance,_nameSpacehandler.Object, _handler.Object);
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

            _nameSpacehandler.Reset();
            _nameSpacehandler.Setup(handler => handler.GetNamespaceDetails(It.IsAny<List<string>>()))
                .Returns((List<string> namespaces) =>
                {
                    var task = new TaskCompletionSource<NamespaceDetails>();
                    task.SetResult(new NamespaceDetails
                    {
                        Package = _packageDetails,
                        Namespaces = new string[]{ "TestNameSpace"},
                    });
                    return new Dictionary<string, Task<NamespaceDetails>>
                    {
                        {"TestNameSpace", task.Task }
                    };
                });
        }

        private List<ProjectDetails> getProjects(string pathToSolution)
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

                return new ProjectDetails
                {
                    ProjectName = p.ProjectName,
                    ProjectFilePath = p.AbsolutePath,
                    ProjectGuid = p.ProjectGuid,
                    ProjectType = p.ProjectType.ToString(),
                    TargetFrameworks = projectParser.GetTargetFrameworks().Select(tfm =>
                    {
                        var framework = NuGetFramework.Parse(tfm);
                        return string.Format("{0} {1}", framework.Framework, NuGetVersion.Parse(framework.Version.ToString()).ToNormalizedString());
                    }).ToList(),
                    PackageReferences = projectParser.GetPackageReferences(),
                    ProjectReferences = projectParser.GetProjectReferences()
                };
            }).Where(p => p != null).ToList();

            return projects;
        }

        [Test]
        public void AnalyzeSolution()
        {
            var result = _PortingAssistantApiAnalysisHandler.AnalyzeSolution(solutionFile, projects);
            Task.WaitAll(result.ProjectApiAnalysisResults.Values.ToArray());
            var values = result.ProjectApiAnalysisResults.Values.First().Result;
            //Assert.AreEqual(projects.First().ProjectPath, values.);
            Assert.AreEqual("Newtonsoft.Json", values.SourceFileAnalysisResults.First().ApiAnalysisResults.First().Invocation.Package.PackageId);
            Assert.AreEqual("11.0.1", values.SourceFileAnalysisResults.First().ApiAnalysisResults.First().Invocation.Package.Version);
            Assert.AreEqual("Newtonsoft.Json.JsonConvert.SerializeObject(object)",
                values.SourceFileAnalysisResults.First().ApiAnalysisResults.First().Invocation.OriginalDefinition);
            Assert.AreEqual(Compatibility.COMPATIBLE, values.SourceFileAnalysisResults.First().ApiAnalysisResults.First().CompatibilityResult);
            Assert.AreEqual("12.0.4", values.SourceFileAnalysisResults.First().ApiAnalysisResults.First().ApiRecommendation.UpgradeVersion);
        }

        [Test]
        public void AnalyzeFaultPath()
        {
            Assert.Throws<AggregateException>(() =>
            {
                var result = _PortingAssistantApiAnalysisHandler.AnalyzeSolution("", projects);
                Task.WaitAll(result.ProjectApiAnalysisResults.Values.ToArray());
            });

            Assert.Throws<AggregateException>(() =>
            {
                var result = _PortingAssistantApiAnalysisHandler.AnalyzeSolution(Path.Combine(solutionFile, "radn.sln"), projects);
                Task.WaitAll(result.ProjectApiAnalysisResults.Values.ToArray());
            });
        }

    }
}
