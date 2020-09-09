using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PortingAssistant.ApiAnalysis;
using PortingAssistant;
using PortingAssistant.Utils;
using PortingAssistant.NuGet;
using PortingAssistant.Model;
using PortingAssistant.Porting;
using PortingAssistant.ApiAnalysis.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;


namespace PortingAssistantAssessmentTest
{
    public class PortingAssistantHandlerTest
    {

        private Mock<IPortingAssistantNuGetHandler> _PortingAssistantNuGetMock;
        private Mock<IPortingAssistantApiAnalysisHandler> _apiAnalysisMock;
        private Mock<IPortingHandler> _portingHandler;
        private PortingAssistantHandler _PortingAssistantHandler;
        private readonly string _solutionFolder = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithProjects");

        private readonly PackageDetails _packageDetails = new PackageDetails
        {
            Name = "Newtonsoft.Json",
            Versions = new SortedSet<string> { "12.0.3", "12.0.4" },
            Api = new ApiDetails[]
            {
                new ApiDetails
                {
                    MethodName = "Setup(Object)",
                    MethodSignature = "Accessibility.Setup(Object)",
                    Targets = new Dictionary<string, SortedSet<string>>
                    {
                        {
                             "netcoreapp3.1", new SortedSet<string> { "12.0.3", "12.0.4" }
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

        private readonly SourceFileAnalysisResult sourceFileAnalysisResult = new SourceFileAnalysisResult
        {
            SourceFileName = "test",
            SourceFilePath = "/test/test",
            ApiAnalysisResults = new List<ApiAnalysisResult>
            {
                new ApiAnalysisResult
                {
                    CompatibilityResult = new Dictionary<string, Compatibility>
                    {
                        { ApiCompatiblity.DEFAULT_TARGET, Compatibility.COMPATIBLE}
                    }
                }
            }
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _PortingAssistantNuGetMock = new Mock<IPortingAssistantNuGetHandler>();
            _apiAnalysisMock = new Mock<IPortingAssistantApiAnalysisHandler>();
            _portingHandler = new Mock<IPortingHandler>();
            _PortingAssistantHandler = new PortingAssistantHandler(
                NullLogger<PortingAssistantHandler>.Instance,
                _PortingAssistantNuGetMock.Object,
                _apiAnalysisMock.Object,
                _portingHandler.Object);
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
                        var taskCompletionSource = new TaskCompletionSource<PackageDetails>();
                        taskCompletionSource.SetResult(
                            _packageDetails
                            );
                        return new Tuple<PackageVersionPair, Task<PackageDetails>>(
                            packageVersion, taskCompletionSource.Task
                        );
                    }).ToDictionary(t => t.Item1, t => t.Item2);
                });

            _apiAnalysisMock.Reset();
            _apiAnalysisMock.Setup(analyzer => analyzer.AnalyzeSolution(It.IsAny<string>(), It.IsAny<List<ProjectDetails>>()))
                .Returns((string solutionFilePath, List<ProjectDetails> projects) =>
                {
                    var taskCompletionSource = new TaskCompletionSource<ProjectApiAnalysisResult>();
                    taskCompletionSource.SetResult(new ProjectApiAnalysisResult {
                        SourceFileAnalysisResults = new List<SourceFileAnalysisResult>
                        {
                            sourceFileAnalysisResult
                        }
                    });

                    return new SolutionApiAnalysisResult
                    {
                        ProjectApiAnalysisResults = projects.Select(p => {
                            return new Tuple<string, Task<ProjectApiAnalysisResult>>(p.ProjectFilePath, taskCompletionSource.Task);
                        }).ToDictionary(t => t.Item1, t => t.Item2)
                    };
                });
        }
        
        [Test]
        public void TestGetSolutionDetails()
        {
            var solutionDetail = _PortingAssistantHandler.GetSolutionDetails(
                Path.Combine(_solutionFolder, "ModernSolution.sln")
            );
            Assert.AreEqual("ModernSolution", solutionDetail.SolutionName);
            Assert.AreEqual(1, solutionDetail.Projects.Find(p => p.ProjectName == "PortingAssistantApi").ProjectReferences.Count);
            Assert.AreEqual(4, solutionDetail.Projects.Find(p => p.ProjectName == "PortingAssistantApi").PackageReferences.Count);
            Assert.AreEqual(8, solutionDetail.Projects.Find(p => p.ProjectName == "Nop.Core").PackageReferences.Count);

            Assert.AreEqual(5, solutionDetail.Projects.Count);
            Assert.Contains("PortingAssistantApi", solutionDetail.Projects.Select(result => result.ProjectName).ToList());

            var project = solutionDetail.Projects.First(project => project.ProjectName.Equals("PortingAssistantApi"));
            Assert.Contains("nunit", project.PackageReferences.Select(dep => dep.PackageId).ToList());
            Assert.Contains(Path.Combine(_solutionFolder, "PortingAssistantAssessment", "PortingAssistantAssessment.csproj"),
                project.ProjectReferences.Select(proj => proj.ReferencePath).ToList());

            project = solutionDetail.Projects.First(project => project.ProjectName.Equals("Nop.Core"));
            Assert.Contains("Autofac", project.PackageReferences.Select(dep => dep.PackageId).ToList());
            Assert.Contains(".NETFramework 4.5.1", project.TargetFrameworks);



        }

        
        [Test]
        public void TestGetSolutionDetailsWithExpection()
        {
            Assert.Throws<PortingAssistantException>(() =>
            {
                var solutionDetails = _PortingAssistantHandler.GetSolutionDetails(Path.Combine(_solutionFolder, "failed.sln"));
            });
        }

        [Test]
        public void TestAnalyzeSolution()
        {
            var results = _PortingAssistantHandler.AnalyzeSolution(Path.Combine(_solutionFolder, "ModernSolution.sln"), new Settings());
            var projctAnalysResult = results.ProjectAnalysisResult.Find(p => p.ProjectName == "Nop.Core");
            var projectApiAnlysisResult = projctAnalysResult.ProjectApiAnalysisResult;
            var packageAnalysisResult = projctAnalysResult.PackageAnalysisResults;

            projectApiAnlysisResult.Wait();
            Assert.AreEqual(sourceFileAnalysisResult, projectApiAnlysisResult.Result.SourceFileAnalysisResults.First());

            Task.WaitAll(packageAnalysisResult.Values.ToArray());
            var packageResult = packageAnalysisResult.First(p => p.Value.Result.PackageVersionPair.PackageId == _packageDetails.Name);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageResult.Value.Result.PackageRecommendation.RecommendedActionType); ;
            var compatibilityinfo = packageResult.Value.Result.CompatibilityResult.GetValueOrDefault(PackageCompatibility.DEFAULT_TARGET);
            Assert.AreEqual(Compatibility.COMPATIBLE, compatibilityinfo.Compatibility);
            Assert.AreEqual("12.0.4", compatibilityinfo.CompatibleVersion.First());
        }
        
        [Test]
        public void TestGetProjectWithCorruptedSlnFile()
        {
            var testSolutionPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithFailedContent", "NopCommerce.sln");

            Assert.Throws<PortingAssistantException>(() =>
            {
                var solutionDetails = _PortingAssistantHandler.GetSolutionDetails(Path.Combine(_solutionFolder, "testSolutionPath"));
            });
        }
        
    }
}