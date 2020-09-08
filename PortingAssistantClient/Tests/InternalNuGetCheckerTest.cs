using NUnit.Framework;
using System.IO;
using System.Xml;
using PortingAssistant.NuGet.InternalNuGetChecker;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using NuGet.Protocol.Core.Types;
using NuGet.Configuration;
using System.Linq;
using System;
using PortingAssistant.Model;
using Settings = NuGet.Configuration.Settings;

namespace PortingAssistantPrivateCompatibilityCheckTests
{
    public class CheckCompatibilityHandlerTest
    {
        IPortingAssistantInternalNuGetCompatibilityHandler checker;
        string tmpSolutionPath;
        IEnumerable<SourceRepository> sourceRepositories;

        private IEnumerable<SourceRepository> GetInternalRepository(string pathToSolution)
        {
            var setting = Settings.LoadDefaultSettings(pathToSolution);
            var sourceRepositoryProvider = new SourceRepositoryProvider(
                new PackageSourceProvider(setting),
                Repository.Provider.GetCoreV3());

            var repositories = sourceRepositoryProvider
                .GetRepositories()
                .Where(r => r.PackageSource.Name.ToLower() != "nuget.org");

            return repositories;
        }

        [SetUp]
        public void Setup()
        {
            checker = new PortingAssistantInternalNuGetCompatibilityHandler(NullLogger<PortingAssistantInternalNuGetCompatibilityHandler>.Instance);
            tmpSolutionPath = createFakeSolutionPath();
            sourceRepositories = GetInternalRepository(tmpSolutionPath);

        }

        [TearDown]
        public void Cleanup()
        {
            Directory.Delete(tmpSolutionPath, true);
        }

        [Test]
        public void TestCompatible()
        {
            var resultTask1 = checker.CheckCompatibilityAsync("Newtonsoft.Json", "12.0.3", "netcoreapp3.1", sourceRepositories);
            var resultTask2 = checker.CheckCompatibilityAsync("Newtonsoft.Json", "12.0.3", "netstandard2.0", sourceRepositories);
            resultTask1.Wait();
            resultTask2.Wait();
            Assert.True(resultTask1.Result.IsCompatible);
            Assert.True(resultTask2.Result.IsCompatible);
        }


        [Test]
        public void TestNotCompatible()
        {
            var resultTask1 = checker.CheckCompatibilityAsync("Newtonsoft.Json", "6.0.1", "netcoreapp3.1", sourceRepositories);
            var resultTask2 = checker.CheckCompatibilityAsync("Newtonsoft.Json", "6.0.1", "netstandard2.0", sourceRepositories);
            resultTask1.Wait();
            resultTask2.Wait();
            Assert.False(resultTask1.Result.IsCompatible);
            Assert.False(resultTask2.Result.IsCompatible);
        }

        [Test]
        public void TestNotExists()
        {
            Assert.ThrowsAsync<PortingAssistantClientException>(async () =>
                 await checker.CheckCompatibilityAsync("Newtonsoft.Json", "12.0.5", "netcoreapp3.1", sourceRepositories));
        }

        [Test]
        public void TestInvalidPath()
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                 await checker.CheckCompatibilityAsync("Newtonsoft.Json", "12.0.3", "netcoreapp3.1", null));
        }

        [Test]
        public void TestInvalidPackage()
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                 await checker.CheckCompatibilityAsync(null, "12.0.3", "netcoreapp3.1", sourceRepositories));
        }


        private string createFakeSolutionPath()
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tmpDir);
            var configFilePath = Path.Combine(tmpDir, "nuget.config");

            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            var testSource = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestXml", "TestNugetRepository");
            //create a local test nuget.config file. extra space in url should be trimmed
            string xml = "<configuration>" +
                         "<config><add key=\"repositoryPath\" value=\"..\\Packages\" /></config>" +
                         "<disabledPackageSources><add key=\"TeamCity\" value=\"true\" /></disabledPackageSources>" +
                         "<packageRestore><add key=\"enabled\" value=\"True\" /></packageRestore>" +
                         "<packageSources>" +
                            "<clear/>" +
                            "<add key=\"nuget.org\" value=\"https://www.nuget.org/api/v2/\"/>" +
                            $"<add key=\"test source\" value=\"{testSource}\"/> " +
                            "<add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" protocolVersion=\"3\"/>" +
                          "</packageSources>" +
                        "</configuration>";

            doc.LoadXml(xml);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            XmlWriter writer = XmlWriter.Create(configFilePath, settings);
            doc.Save(writer);
            return tmpDir;

        }
    }
}