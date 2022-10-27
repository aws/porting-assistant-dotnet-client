using System.Collections.Generic;
using NUnit.Framework;
using System.IO;
using PortingAssistant.Client.Porting;
using PortingAssistant.Client.PortingProjectFile;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;
using PortingAssistant.Client.Model;
using NuGet.Frameworks;
using NuGet.Versioning;
using PortingAssistant.Client.Client.FileParser;
using System;
using PortingAssistant.Client.Common.Model;
using PortingAssistant.Client.Common.Utils;

namespace PortingAssistant.Client.Tests
{
    public class PortingAssistantPortingTest
    {
        private string _tmpDirectory;
        private string _tmpProjectPath;
        private string _tmpSolutionDirectory;
        private string _tmpSolutionFileName;
        private IPortingHandler _portingHandler;
        private IPortingProjectFileHandler _portingProjectFileHandler;

        [SetUp]
        public void Setup()
        {
            _portingProjectFileHandler = new PortingProjectFileHandler(NullLogger<PortingProjectFileHandler>.Instance);
            _portingHandler = new PortingHandler(_portingProjectFileHandler);

            var solutionDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestXml", "TestPorting");
            _tmpDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestXml", "TmpDirectory");
            DirectoryCopy(solutionDirectory, _tmpDirectory, true);

            _tmpSolutionDirectory = Path.Combine(_tmpDirectory, "src");
            _tmpSolutionFileName = Path.Combine(_tmpSolutionDirectory, "NopCommerce.sln");
            _tmpProjectPath = Path.Combine(_tmpSolutionDirectory, "Libraries", "Nop.Core", "Nop.Core.csproj");
        }

        [TearDown]
        public void Cleanup()
        {
            Directory.Delete(_tmpDirectory, true);
        }

        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (var fileInfo in files)
            {
                string tempPath = Path.Combine(destDirName, fileInfo.Name);
                fileInfo.CopyTo(tempPath, false);
            }

            if (copySubDirs)
            {
                foreach (var subDir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subDir.Name);
                    DirectoryCopy(subDir.FullName, tempPath, true);
                }
            }
        }

        private List<ProjectDetails> GetProjects(string pathToSolution)
        {
            var solution = SolutionFile.Parse(pathToSolution);

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
                    TargetFrameworks = projectParser.GetTargetFrameworks().ConvertAll(tfm =>
                    {
                        var framework = NuGetFramework.Parse(tfm);
                        return string.Format("{0} {1}", framework.Framework, NuGetVersion.Parse(framework.Version.ToString()).ToNormalizedString());
                    }),
                    PackageReferences = projectParser.GetPackageReferences()
                };
            }).Where(p => p != null).ToList();

            return projects;
        }

        [Test]
        public void ProjectCompatabilityResultTest()
        {
            ProjectCompatibilityResult projectCompatibilityResult = new ProjectCompatibilityResult();
            var _testPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestXml", "TestPorting");
            projectCompatibilityResult.ProjectPath = _testPath;
            projectCompatibilityResult.IsPorted = true;
            var expectedStr = "Ported Project Compatibilities for D:\\repos\\porting-assistant-dotnet-client\\tests\\PortingAssistant.Client.UnitTests\\bin\\Debug\\net6.0\\TestXml\\TestPorting:\r\nAnnotation: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0\r\nMethod: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0\r\nDeclaration: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0\r\nEnum: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0\r\nStruct: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0\r\n";
            Assert.AreEqual(projectCompatibilityResult.ToString(), expectedStr);
            projectCompatibilityResult.IsPorted = false;
            expectedStr = "Analyzed Project Compatibilities for D:\\repos\\porting-assistant-dotnet-client\\tests\\PortingAssistant.Client.UnitTests\\bin\\Debug\\net6.0\\TestXml\\TestPorting:\r\nAnnotation: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0\r\nMethod: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0\r\nDeclaration: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0\r\nEnum: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0\r\nStruct: Compatible:0, Incompatible:0, Unknown:0, Deprecated:0, Actions:0\r\n";
            Assert.AreEqual(projectCompatibilityResult.ToString(), expectedStr);
        }

        [Test]
        public void PortingProjectSucceedsWithOriginalVerson()
        {
            var result = _portingHandler.ApplyPortProjectFileChanges
                (
                GetProjects(_tmpSolutionFileName),
                _tmpSolutionFileName,
                "netcoreapp3.1",
                new Dictionary<string, Tuple<string, string>> { { "Newtonsoft.Json", new Tuple<string, string>("9.0.0", "12.0.3") } });

            Assert.True(result[0].Success);
            Assert.AreEqual(_tmpProjectPath, result[0].ProjectFile);
            Assert.AreEqual("Nop.Core", result[0].ProjectName);

            var portResult = GetProjects(_tmpSolutionFileName).Find(package => package.ProjectName == "Nop.Core");
            Assert.AreEqual(_tmpProjectPath, portResult.ProjectFilePath);
            Assert.AreEqual(".NETCoreApp 3.1.0", portResult.TargetFrameworks[0]);
            Assert.AreEqual(
                new PackageVersionPair
                {
                    PackageId = "Newtonsoft.Json",
                    Version = "12.0.3"
                },
                portResult.PackageReferences.Find(nugetPackage => nugetPackage.PackageId == "Newtonsoft.Json"));
        }

        [Test]
        public void PortingMissingProjectFailsWithErrorMessage()
        {
            var somePath = "randomPath";
            var result = _portingHandler.ApplyPortProjectFileChanges(
                new List<ProjectDetails>
                {
                    new ProjectDetails
                    {
                        ProjectFilePath = somePath,
                        PackageReferences = new List<PackageVersionPair>
                        {
                            new PackageVersionPair
                            {
                                PackageId = "Newtonsoft",
                                Version = "9.0.0"
                            }
                        }
                    }
                },
                _tmpSolutionFileName,
                "netcoreapp3.1.0",
                new Dictionary<string, Tuple<string, string>>
                {
                    { "Newtonsoft",new Tuple<string, string>("9.0.0", "12.0.3") }
                });

            Assert.False(result[0].Success);
            Assert.AreEqual(somePath, result[0].ProjectFile);
            Assert.AreEqual("File not found.", result[0].Message);
        }

        [Test]
        public void PortingProjectWithCorruptFile()
        {
            var solutionDir = Path.Combine(_tmpDirectory, "corrupt");
            var corruptSolutionFilePath = Path.Combine(solutionDir, "CorruptSolution.sln");
            var corruptProjectFilePath = Path.Combine(solutionDir, "CorruptProject.csproj");
            var result = _portingHandler.ApplyPortProjectFileChanges(
                new List<ProjectDetails>
                {
                    new ProjectDetails
                    {
                        ProjectFilePath = corruptProjectFilePath,
                        PackageReferences = new List<PackageVersionPair>
                        {
                            new PackageVersionPair
                            {
                                PackageId = "Newtonsoft",
                                Version = "9.0.0"
                            }
                        }
                    }
                },
                corruptSolutionFilePath,
                "netcoreapp3.1.0",
                new Dictionary<string, Tuple<string, string>>
                {
                    ["Newtonsoft"] = new Tuple<string, string>("9.0.0", "12.0.3")
                });

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void WriteAccessCheckTest()
        {

             var res= FileSystemAccess.CheckWriteAccessForDirectory(_tmpDirectory);
             Assert.AreEqual("", res);

        }
    }
}