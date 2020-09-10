﻿using System.Collections.Generic;
using NUnit.Framework;
using System.IO;
using PortingAssistant.Porting;
using PortingAssistant.PortingProjectFile;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;
using PortingAssistant.Model;
using NuGet.Frameworks;
using NuGet.Versioning;
using PortingAssistant.Handler.FileParser;

namespace Tests
{
    public class PortingAssistantPortingTest
    {
        private string tmpDirectory;
        private string tmpProjectPath;
        private string tmpSolutionDirectory;
        private IPortingHandler _portingHandler;
        private IPortingProjectFileHandler _portingProjectFileHandler;

        [SetUp]
        public void Setup()
        {
            _portingProjectFileHandler = new PortingProjectFileHandler(NullLogger<PortingProjectFileHandler>.Instance);
            _portingHandler = new PortingHandler(NullLogger<PortingHandler>.Instance, _portingProjectFileHandler);

            var solutionDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestXml", "TestPorting");
            tmpDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestXml", "testDircteory");
            DirectoryCopy(solutionDirectory, tmpDirectory, true);
            tmpSolutionDirectory = Path.Combine(tmpDirectory, "src");
            tmpProjectPath = Path.Combine(tmpSolutionDirectory, "Libraries", "Nop.Core", "Nop.Core.csproj");
            

        }

        [TearDown]
        public void Cleanup()
        {
            Directory.Delete(tmpDirectory, true);
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
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
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
        public void Porting_Success()
        {
            var result = _portingHandler.ApplyPortProjectFileChanges
                (
                new List<string> { tmpProjectPath },
                tmpSolutionDirectory,
                "netcoreapp3.1.0",
                new Dictionary<string, string> {["Newtonsoft.Json"] = "12.0.3"
                });

            Assert.True(result[0].Success);
            Assert.AreEqual(tmpProjectPath, result[0].ProjectFile);
            Assert.AreEqual("Nop.Core", result[0].ProjectName);

            var portResult = getProjects(Path.Combine(tmpSolutionDirectory, "NopCommerce.sln")).Find(package => package.ProjectName == "Nop.Core");
            Assert.AreEqual(tmpProjectPath, portResult.ProjectFilePath);
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
        public void Porting_Failed()
        {
            var result = _portingHandler.ApplyPortProjectFileChanges
                (
                new List<string> { "randompath" },
                tmpSolutionDirectory,
                "netcoreapp3.1.0",
                new Dictionary<string, string>
                {
                    ["Newtonsoft"] = "12.0.6"
                });

            Assert.False(result[0].Success);
            Assert.AreEqual("randompath", result[0].ProjectFile);
            Assert.AreEqual("File not found.", result[0].Message);
        }

        [Test]
        public void Porting_withCorruptFile()
        {
            
            var solutionDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestXml", "testDircteory", "corrupt");
            var projectDir = Path.Combine(solutionDir, "Nop.Core.csproj");
            var result = _portingHandler.ApplyPortProjectFileChanges
                (
                new List<string> { projectDir },
                Path.Combine(solutionDir, "NopCommerce.sln"),
                "netcoreapp3.1.0",
                new Dictionary<string, string>
                {
                    ["Newtonsoft"] = "12.0.6"
                });

            Assert.AreEqual(1, result.Count);
        }

    }
}