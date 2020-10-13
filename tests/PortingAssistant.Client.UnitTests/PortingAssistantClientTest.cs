﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using PortingAssistant.Client.Analysis;
using PortingAssistant.Client.Client;
using PortingAssistant.Client.Utils;
using PortingAssistant.Client.NuGet;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.Porting;
using PortingAssistant.Client.Analysis.Utils;
using Microsoft.Build.Construction;

namespace PortingAssistant.Client.Tests
{
    public class PortingAssistantHandlerTest
    {
        private Mock<IPortingAssistantAnalysisHandler> _apiAnalysisHandlerMock;
        private Mock<IPortingHandler> _portingHandlerMock;
        private PortingAssistantClient _portingAssistantClient;
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

        private readonly SourceFileAnalysisResult _sourceFileAnalysisResult = new SourceFileAnalysisResult
        {
            SourceFileName = "test",
            SourceFilePath = "/test/test",
            ApiAnalysisResults = new List<ApiAnalysisResult>
            {
                new ApiAnalysisResult
                {
                    CompatibilityResults = new Dictionary<string, CompatibilityResult>
                    {
                        { ApiCompatiblity.DEFAULT_TARGET, new CompatibilityResult{
                            Compatibility = Compatibility.COMPATIBLE,
                            CompatibleVersions = new List<string>{ "12.0.3", "12.0.4" }
                        } }
                    }
                }
            }
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _apiAnalysisHandlerMock = new Mock<IPortingAssistantAnalysisHandler>();
            _portingHandlerMock = new Mock<IPortingHandler>();
            _portingAssistantClient = new PortingAssistantClient(
                NullLogger<PortingAssistantClient>.Instance,
                _apiAnalysisHandlerMock.Object,
                _portingHandlerMock.Object);
        }

        [SetUp]
        public void SetUp()
        {

            _apiAnalysisHandlerMock.Reset();
            _apiAnalysisHandlerMock.Setup(analyzer => analyzer.AnalyzeSolution(It.IsAny<string>(), It.IsAny<List<string>>()))
                .Returns((string solutionFilePath, List<string> projects) =>
                {
                    return Task.Run(() => projects.Select(project =>
                    {
                        var package = new PackageVersionPair
                        {
                            PackageId = "Newtonsoft.Json",
                            Version = "11.0.1"
                        };
                        var packageAnalysisResult = Task.Run(() => new PackageAnalysisResult
                        {
                            PackageVersionPair = package,
                            CompatibilityResults = new Dictionary<string, CompatibilityResult>
                            {
                                {"netcoreapp3.1", new CompatibilityResult{
                                    Compatibility = Compatibility.COMPATIBLE,
                                    CompatibleVersions = new List<string>
                                    {
                                        "12.0.3", "12.0.4"
                                    }
                                }}
                            },
                            Recommendations = new Recommendations
                            {
                                RecommendedActions = new List<RecommendedAction>
                                {
                                    new RecommendedAction
                                    {
                                        RecommendedActionType = RecommendedActionType.UpgradePackage,
                                        Description = "12.0.3"
                                    }
                                }
                            }
                        });

                        var projectAnalysisResult = new ProjectAnalysisResult
                        {
                            ProjectName = Path.GetFileNameWithoutExtension(project),
                            ProjectFilePath = project,
                            PackageAnalysisResults = new Dictionary<PackageVersionPair, Task<PackageAnalysisResult>>
                            {
                                { package, packageAnalysisResult }
                            },
                            SourceFileAnalysisResults = new List<SourceFileAnalysisResult>
                            {
                                _sourceFileAnalysisResult
                            },
                            ProjectGuid = "xxx",
                            ProjectType = SolutionProjectType.KnownToBeMSBuildFormat.ToString()
                        };

                        return new KeyValuePair<string, ProjectAnalysisResult>(project, projectAnalysisResult);
                    }).ToDictionary(k => k.Key, v => v.Value));
                });
        }

        [Test]
        public void AnalyzeSolutionWithProjectsSucceeds()
        {
            var results = _portingAssistantClient.AnalyzeSolutionAsync(Path.Combine(_solutionFolder, "SolutionWithProjects.sln"), new AnalyzerSettings());
            results.Wait();
            var projectAnalysisResult = results.Result.ProjectAnalysisResults.Find(p => p.ProjectName == "Nop.Core");
            var sourceFileAnalysisResults = projectAnalysisResult.SourceFileAnalysisResults;
            var packageAnalysisResult = projectAnalysisResult.PackageAnalysisResults;

            Assert.AreEqual(_sourceFileAnalysisResult, sourceFileAnalysisResults.First());

            Task.WaitAll(packageAnalysisResult.Values.ToArray());
            var packageResult = packageAnalysisResult.First(p => p.Value.Result.PackageVersionPair.PackageId == _packageDetails.Name);
            Assert.AreEqual(RecommendedActionType.UpgradePackage, packageResult.Value.Result.Recommendations.RecommendedActions.First().RecommendedActionType); ;
            var compatibilityResult = packageResult.Value.Result.CompatibilityResults.GetValueOrDefault(PackageCompatibility.DEFAULT_TARGET);
            Assert.AreEqual(Compatibility.COMPATIBLE, compatibilityResult.Compatibility);
            Assert.AreEqual("12.0.3", compatibilityResult.CompatibleVersions.First());

            var solutionDetail = results.Result.SolutionDetails;
            Assert.AreEqual("SolutionWithProjects", solutionDetail.SolutionName);

            Assert.AreEqual(5, solutionDetail.Projects.Count);
            Assert.Contains("PortingAssistantApi", solutionDetail.Projects.Select(result => result.ProjectName).ToList());

            var project = solutionDetail.Projects.First(project => project.ProjectName.Equals("PortingAssistantApi"));
            Assert.AreEqual("xxx", project.ProjectGuid);
            Assert.AreEqual(SolutionProjectType.KnownToBeMSBuildFormat.ToString(), project.ProjectType);
        }

        [Test]
        public void GetProjectWithCorruptedSolutionFileThrowsException()
        {
            var testSolutionPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithFailedContent", "NopCommerce.sln");

            Assert.Throws<AggregateException>(() =>
            {
                var result = _portingAssistantClient.AnalyzeSolutionAsync(testSolutionPath, new AnalyzerSettings());
                result.Wait();
            });
        }
    }
}