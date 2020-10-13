using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NUnit.Framework;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.NuGet;
using PortingAssistant.Client.NuGet.Interfaces;
using PortingAssistant.Client.NuGet.InternalNuGet;
using ILogger = NuGet.Common.ILogger;

namespace PortingAssistant.Client.Tests
{
    public class PortingAssistantNuGetHandlerTest
    {
        private Mock<IHttpService> _httpService;
        private Mock<IPortingAssistantInternalNuGetCompatibilityHandler> _internalNuGetCompatibilityHandlerMock;
        private Mock<InternalPackagesCompatibilityChecker> _internalPackagesCompatibilityChecker;
        private ExternalPackagesCompatibilityChecker _externalPackagesCompatibilityChecker;
        private PortabilityAnalyzerCompatibilityChecker _portabilityAnalyzerCompatibilityChecker;
        private SdkCompatibilityChecker _sdkCompatibilityChecker;
        private Mock<ILogger<PortingAssistantNuGetHandler>> _loggerMock;
        private readonly string _testSolutionDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestXml", "SolutionWithNugetConfigFile");

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

        private readonly Dictionary<string, string> _manifest = new Dictionary<string, string> { { "Newtonsoft.Json", "microsoftlibs.newtonsoft.json.json" } };
        private async Task<InternalNuGetCompatibilityResult> GetCompatibilityResult(
            int timeout, bool compatibility)
        {
            await Task.Delay(timeout);

            return new InternalNuGetCompatibilityResult
            {
                CompatibleDlls = null,
                IsCompatible = compatibility,
                IncompatibleDlls = null,
                Source = "nuget.woot.com",
                DependencyPackages = null
            };
        }

        private IEnumerable<SourceRepository> GetInternalRepository()
        {
            var mockResourceRepositories = new List<SourceRepository>();
            var mockResourceRepository = new Mock<SourceRepository>();
            var mockResource = new Mock<FindPackageByIdResource>();

            mockResource.Reset();
            mockResource.Setup(source => source.DoesPackageExistAsync(
                It.IsAny<string>(),
                It.IsAny<NuGetVersion>(),
                It.IsAny<SourceCacheContext>(),
                It.IsAny<ILogger>(),
                It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(5);
                    return true;
                });

            mockResourceRepository.Reset();
            mockResourceRepository.Setup(source => source.GetResourceAsync<
                FindPackageByIdResource>())
                .Returns(async () =>
                {
                    await Task.Delay(5);
                    return mockResource.Object;
                });

            mockResourceRepositories.Add(mockResourceRepository.Object);
            return mockResourceRepositories.AsEnumerable();
        }

        private IEnumerable<SourceRepository> GetInternalRepositoryNotExist()
        {
            var mockRepositories = new List<SourceRepository>();
            var mockResourceRepository = new Mock<SourceRepository>();
            var mockResource = new Mock<FindPackageByIdResource>();

            mockResource.Reset();
            mockResource.Setup(source => source.DoesPackageExistAsync(
                It.IsAny<string>(),
                It.IsAny<NuGetVersion>(),
                It.IsAny<SourceCacheContext>(),
                It.IsAny<ILogger>(),
                It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(5);
                    return false;
                });

            mockResourceRepository.Reset();
            mockResourceRepository.Setup(source => source.GetResourceAsync<
                FindPackageByIdResource>())
                .Returns(async () =>
                {
                    await Task.Delay(5);
                    return mockResource.Object;
                });

            mockRepositories.Add(mockResourceRepository.Object);
            return mockRepositories.AsEnumerable();
        }

        private IEnumerable<SourceRepository> GetInternalRepositoryThrowsException(Exception exception)
        {
            var mockRepositories = new List<SourceRepository>();
            var mockResourceRepository = new Mock<SourceRepository>();
            var mockResource = new Mock<FindPackageByIdResource>();

            mockResource.Reset();
            mockResource.Setup(source => source.DoesPackageExistAsync(
                It.IsAny<string>(),
                It.IsAny<NuGetVersion>(),
                It.IsAny<SourceCacheContext>(),
                It.IsAny<ILogger>(),
                It.IsAny<CancellationToken>()))
                .Throws(exception);

            mockResourceRepository.Reset();
            mockResourceRepository.Setup(source => source.GetResourceAsync<
                FindPackageByIdResource>())
                .Returns(async () =>
                {
                    await Task.Delay(5);
                    return mockResource.Object;
                });

            mockRepositories.Add(mockResourceRepository.Object);
            return mockRepositories.AsEnumerable();
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            //httpMessageHandler = new Mock<HttpMessageHandler>
            _httpService = new Mock<IHttpService>();
            _internalNuGetCompatibilityHandlerMock = new Mock<IPortingAssistantInternalNuGetCompatibilityHandler>();
        }

        [SetUp]
        public void Setup()
        {
            _httpService.Reset();

            _httpService
                .Setup(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()))
                .Returns(async (string key) =>
                {
                    await Task.Delay(1);
                    var stream = new MemoryStream();
                    var writer = new StreamWriter(stream);
                    if (key.Equals("microsoftlibs.namespace.lookup.json"))
                    {
                        var test = JsonConvert.SerializeObject(_manifest);
                        writer.Write(test);
                        writer.Flush();
                        stream.Position = 0;
                        var outputStream = new MemoryStream();
                        stream.CopyTo(outputStream);
                        outputStream.Position = 0;
                        return outputStream;
                    }
                    else
                    {
                        var test = JsonConvert.SerializeObject(new Dictionary<string, PackageDetails> { { "Package", _packageDetails } });
                        writer.Write(test);
                        writer.Flush();
                        stream.Position = 0;
                        var outputStream = new MemoryStream();
                        var gzipStream = new GZipStream(outputStream, CompressionLevel.Fastest);
                        stream.CopyTo(gzipStream);
                        gzipStream.Flush();
                        outputStream.Position = 0;
                        return outputStream;
                    }
                });

            _internalNuGetCompatibilityHandlerMock.Reset();

            _internalNuGetCompatibilityHandlerMock
                .Setup(checker => checker.CheckCompatibilityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<SourceRepository>>()))
                .Returns((string packageName, string version, string targetFramework, IEnumerable<SourceRepository> sourceRepositories) =>
                {
                    return GetCompatibilityResult(200, true);
                });


            _externalPackagesCompatibilityChecker = new ExternalPackagesCompatibilityChecker(
                _httpService.Object,
                NullLogger<ExternalPackagesCompatibilityChecker>.Instance
                );

            _portabilityAnalyzerCompatibilityChecker = new PortabilityAnalyzerCompatibilityChecker(
                _httpService.Object,
                NullLogger<PortabilityAnalyzerCompatibilityChecker>.Instance
                );

            _sdkCompatibilityChecker = new SdkCompatibilityChecker(
                _httpService.Object,
                NullLogger<SdkCompatibilityChecker>.Instance
                );

            _portabilityAnalyzerCompatibilityChecker = new PortabilityAnalyzerCompatibilityChecker(
                _httpService.Object,
                NullLogger<PortabilityAnalyzerCompatibilityChecker>.Instance
                );

            _internalPackagesCompatibilityChecker = new Mock<InternalPackagesCompatibilityChecker>(
                _internalNuGetCompatibilityHandlerMock.Object,
                NullLogger<InternalPackagesCompatibilityChecker>.Instance);


            _internalPackagesCompatibilityChecker.Reset();
            _internalPackagesCompatibilityChecker.Setup(checker => checker.GetInternalRepositories(
                It.IsAny<string>())).Returns(() =>
                {
                    return GetInternalRepository();
                });
        }

        private IPortingAssistantNuGetHandler GetExternalNuGetHandler()
        {
            var checkers = new List<ICompatibilityChecker>() { _externalPackagesCompatibilityChecker };
            return new PortingAssistantNuGetHandler(
                    NullLogger<PortingAssistantNuGetHandler>.Instance,
                    checkers.AsEnumerable()
                    );
        }

        private IPortingAssistantNuGetHandler GetInternalNuGetHandler()
        {
            var checkers = new List<ICompatibilityChecker>() { _internalPackagesCompatibilityChecker.Object };
            return new PortingAssistantNuGetHandler(
                    NullLogger<PortingAssistantNuGetHandler>.Instance,
                    checkers.AsEnumerable()
                    );
        }

        private IPortingAssistantNuGetHandler GetNamespaceNugetHandler()
        {
            var checkers = new List<ICompatibilityChecker>() { _sdkCompatibilityChecker };
            return new PortingAssistantNuGetHandler(
                    NullLogger<PortingAssistantNuGetHandler>.Instance,
                    checkers.AsEnumerable()
                    );
        }

        private IPortingAssistantNuGetHandler GetPortabilityAnalzyerHandler()
        {
            var checkers = new List<ICompatibilityChecker>() { _portabilityAnalyzerCompatibilityChecker };
            return new PortingAssistantNuGetHandler(
                    NullLogger<PortingAssistantNuGetHandler>.Instance,
                    checkers.AsEnumerable()
                    );
        }

        private IPortingAssistantNuGetHandler GetBothNuGetHandler()
        {
            var checkers = new List<ICompatibilityChecker>() { _externalPackagesCompatibilityChecker, _internalPackagesCompatibilityChecker.Object };
            return new PortingAssistantNuGetHandler(
                    NullLogger<PortingAssistantNuGetHandler>.Instance,
                    checkers.AsEnumerable()
                    );
        }

        private IPortingAssistantNuGetHandler GetCheckerWithException()
        {
            var checker = new Mock<ICompatibilityChecker>();
            checker.Setup(checker => checker.CheckAsync(
                It.IsAny<List<PackageVersionPair>>(),
                It.IsAny<string>()))
                .Throws(new Exception("test"));

            _loggerMock = new Mock<ILogger<PortingAssistantNuGetHandler>>();

            _loggerMock.Reset();

            _loggerMock.Setup(_ => _.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsValueType>(),
                It.IsAny<Exception>(),
                (Func<It.IsValueType, Exception, string>)It.IsAny<object>()));

            var checkers = new List<ICompatibilityChecker>() { checker.Object };
            return new PortingAssistantNuGetHandler(
                    _loggerMock.Object,
                    checkers.AsEnumerable()
                    );
        }

        private ExternalPackagesCompatibilityChecker GetExternalPackagesCompatibilityChecker()
        {
            var externalChecker = new ExternalPackagesCompatibilityChecker(
                _httpService.Object,
                NullLogger<ExternalPackagesCompatibilityChecker>.Instance
            );

            return externalChecker;
        }

        private void SetMockTransferUtility(PackageDetails packageDetails)
        {
            _httpService.Reset();
            _httpService
                .Setup(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()))
                .Returns(async (string key) =>
                {
                    await Task.Delay(1);
                    var stream = new MemoryStream();
                    var writer = new StreamWriter(stream);
                    if (key.Equals("microsoftlibs.namespace.lookup.json"))
                    {
                        var test = JsonConvert.SerializeObject(_manifest);
                        writer.Write(test);
                        writer.Flush();
                        stream.Position = 0;
                        var outputStream = new MemoryStream();
                        stream.CopyTo(outputStream);
                        outputStream.Position = 0;
                        return outputStream;
                    }
                    else
                    {
                        var test = JsonConvert.SerializeObject(new Dictionary<string, PackageDetails> { { "Package", packageDetails } });
                        writer.Write(test);
                        writer.Flush();
                        stream.Position = 0;
                        var outputStream = new MemoryStream();
                        var gzipStream = new GZipStream(outputStream, CompressionLevel.Fastest);
                        stream.CopyTo(gzipStream);
                        gzipStream.Flush();
                        outputStream.Position = 0;
                        return outputStream;
                    }
                });
        }

        [Test]
        public void GetNugetPackagesWithExternalNugetRepositorySucceeds()
        {
            var handler = GetExternalNuGetHandler();
            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3", PackageSourceType = PackageSourceType.NUGET }
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());

            Assert.AreEqual(_packageDetails.Name, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(_packageDetails.Api.Count(), resultTasks.Values.First().Result.Api.Count());
            Assert.AreEqual(_packageDetails.Targets.Count(), resultTasks.Values.First().Result.Targets.Count());
            Assert.AreEqual(_packageDetails.Versions.Count(), resultTasks.Values.First().Result.Versions.Count());
        }

        [Test]
        public void GetPackageWithSdkRepositorySucceeds()
        {
            var handler = GetNamespaceNugetHandler();
            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3", PackageSourceType = PackageSourceType.SDK }
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());

            Assert.AreEqual(_packageDetails.Name, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(_packageDetails.Api.Count(), resultTasks.Values.First().Result.Api.Count());
            Assert.AreEqual(_packageDetails.Targets.Count(), resultTasks.Values.First().Result.Targets.Count());
            Assert.AreEqual(_packageDetails.Versions.Count(), resultTasks.Values.First().Result.Versions.Count());
        }

        [Test]
        public void GetPackageWithPortabilityAnalyzerCatalogSucceeds()
        {
            var handler = GetPortabilityAnalzyerHandler();
            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3", PackageSourceType = PackageSourceType.SDK }
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());

            Assert.AreEqual(_packageDetails.Name, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(_packageDetails.Api.Count(), resultTasks.Values.First().Result.Api.Count());
            Assert.AreEqual(_packageDetails.Targets.Count(), resultTasks.Values.First().Result.Targets.Count());
            Assert.AreEqual(_packageDetails.Versions.Count(), resultTasks.Values.First().Result.Versions.Count());
        }

        [Test]
        public void GetNugetPackagesFromInternalNugetRepositorySucceeds()
        {
            var handler = GetInternalNuGetHandler();

            _internalNuGetCompatibilityHandlerMock.Reset();

            _internalNuGetCompatibilityHandlerMock
                .Setup(checker => checker.CheckCompatibilityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<SourceRepository>>()))
                .Returns((string packageName, string version, string targetFramework, IEnumerable<SourceRepository> sourceRepositories) =>
                {
                    return GetCompatibilityResult(1, true);
                });

            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3" }
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());

            Assert.AreEqual(packages.First().PackageId, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(packages.First().Version, resultTasks.Values.First().Result.Targets["netcoreapp3.1"].First());
        }

        [Test]
        public void GetNugetPackagesWithIncompatibleExternalNugetRepositorySucceeds()
        {
            var handler = GetExternalNuGetHandler();
            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.2", PackageSourceType = PackageSourceType.NUGET }
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());

            Assert.AreEqual(_packageDetails.Name, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(_packageDetails.Api.Count(), resultTasks.Values.First().Result.Api.Count());
            Assert.AreEqual(_packageDetails.Targets.Count(), resultTasks.Values.First().Result.Targets.Count());
            Assert.AreEqual(_packageDetails.Versions.Count(), resultTasks.Values.First().Result.Versions.Count());
        }

        [Test]
        public void GetNugetPackagesWithIncompatibleInternalNugetRepositorySucceeds()
        {
            var handler = GetInternalNuGetHandler();
            _internalNuGetCompatibilityHandlerMock.Reset();

            _internalNuGetCompatibilityHandlerMock
                .Setup(checker => checker.CheckCompatibilityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<SourceRepository>>()))
                .Returns((string packageName, string version, string targetFramework, IEnumerable<SourceRepository> sourceRepositories) =>
                {
                    return GetCompatibilityResult(1, false);
                });

            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.2" }
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());

            Assert.AreEqual(packages.First().PackageId, resultTasks.Values.First().Result.Name);
            Assert.AreEqual("netcoreapp3.1", resultTasks.Values.First().Result.Targets.First().Key);
            Assert.AreEqual(0, resultTasks.Values.First().Result.Targets["netcoreapp3.1"].Count);
        }

        [Test]
        public void GetNugetPackagesWithNonexistentPackageInInternalNugetRepositoryThrowsException()
        {
            _internalPackagesCompatibilityChecker.Reset();
            _internalPackagesCompatibilityChecker.Setup(checker => checker.GetInternalRepositories(
                It.IsAny<string>())).Returns(() =>
                {
                    return GetInternalRepositoryNotExist();
                });

            _httpService.Reset();
            _httpService
                .Setup(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()))
                .Throws(new Exception());

            var handler = GetBothNuGetHandler();

            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.5", PackageSourceType = PackageSourceType.NUGET }
            };

            Assert.Throws<AggregateException>(() =>
            {
                var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
                Task.WaitAll(resultTasks.Values.ToArray());
            });
        }

        [Test]
        public void TransferUtilityOpenStreamTriesNextPackageVersionOnException()
        {
            _internalPackagesCompatibilityChecker.Reset();
            _internalPackagesCompatibilityChecker.Setup(checker => checker.GetInternalRepositories(
                It.IsAny<string>())).Returns(() =>
                {
                    return GetInternalRepositoryNotExist();
                });

            _httpService.Reset();
            _httpService
                .Setup(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()))
                .Throws(new Exception());

            var handler = GetBothNuGetHandler();

            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.5", PackageSourceType = PackageSourceType.NUGET },
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.6", PackageSourceType = PackageSourceType.NUGET }
            };

            try
            {
                var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
                Task.WaitAll(resultTasks.Values.ToArray());
            }
            catch (Exception)
            { }

            try
            {
                var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
                Task.WaitAll(resultTasks.Values.ToArray());
            }
            catch (Exception)
            { }

            _httpService.Verify(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void TransferUtilityOpenStreamCalledOncePerPackage()
        {
            var handler = GetExternalNuGetHandler();

            var packages = new List<PackageVersionPair>() {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3", PackageSourceType = PackageSourceType.NUGET },
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.4" , PackageSourceType = PackageSourceType.NUGET}
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());
            _httpService.Verify(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()), Times.Exactly(1));
        }

        [Test]
        public void TransferUtilityOpenStreamResultsAreCached()
        {
            var handler = GetExternalNuGetHandler();

            var packages = new List<PackageVersionPair> {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3", PackageSourceType = PackageSourceType.NUGET },
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.4", PackageSourceType = PackageSourceType.NUGET }
            };

            var resultTasks1 = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
            var resultTasks2 = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
            Task.WaitAll(resultTasks1.Values.ToArray());
            Task.WaitAll(resultTasks2.Values.ToArray());

            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());

            // Doesn't fire another request when requesting for same package.
            _httpService.Verify(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()), Times.Exactly(1));
        }

        [Test]
        public void PackageDownloadRequestWithInvalidJsonResponseThrowsException()
        {
            _httpService.Reset();
            _httpService
                .Setup(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()))
                .Returns(async (string key) =>
                {
                    await Task.Delay(1);
                    MemoryStream stream = new MemoryStream();
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write("INVALID");
                    writer.Flush();
                    writer.BaseStream.Position = 0;
                    return writer.BaseStream;
                });

            var externalChecker = GetExternalPackagesCompatibilityChecker();

            var packageVersionPair = new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.5", PackageSourceType = PackageSourceType.NUGET };
            var packages = new List<PackageVersionPair>()
            {
                packageVersionPair
            };
            Assert.Throws<AggregateException>(() =>
            {
                var resultTasks = externalChecker.CheckAsync(packages, null);
                Task.WaitAll(resultTasks.Values.ToArray());
            });
        }

        [Test]
        public void CompatibilityCheckOfMissingExternalPackageThrowsException()
        {
            SetMockTransferUtility(new PackageDetails());

            var externalPackagesCompatibilityChecker = GetExternalPackagesCompatibilityChecker();

            var packageVersionPair = new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.5", PackageSourceType = PackageSourceType.NUGET };
            var packages = new List<PackageVersionPair>()
            {
                packageVersionPair
            };

            var resultTasks = externalPackagesCompatibilityChecker.CheckAsync(packages, null);

            _loggerMock.Verify(_ => _.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsValueType>(),
                It.IsAny<Exception>(),
                (Func<It.IsValueType, Exception, string>)It.IsAny<object>()), Times.Once);

            Assert.Throws<AggregateException>(() =>
            {
                Task.WaitAll(resultTasks.Values.ToArray());
            });
        }

        [Test]
        public void CompatibilityCheckerLoggerLogsErrorsInGetNugetPackages()
        {
            var handler = GetCheckerWithException();
            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3" }
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));
            _loggerMock.Verify(_ => _.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsValueType>(),
                It.IsAny<Exception>(),
                (Func<It.IsValueType, Exception, string>)It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void GetInternalRepositoryReturnsCorrectInternalRepositories()
        {
            var compatibilityChecker = new InternalPackagesCompatibilityChecker(
                _internalNuGetCompatibilityHandlerMock.Object,
                NullLogger<InternalPackagesCompatibilityChecker>.Instance);

            var result = compatibilityChecker.GetInternalRepositories(Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln")).ToList();

            Assert.AreEqual("nuget.woot.com", result.First().PackageSource.Name.ToLower());
        }

        [Test]
        public void InternalPackagesLoggerLogsExceptions()
        {
            var checker = _internalPackagesCompatibilityChecker.Object;
            var repositories = GetInternalRepositoryThrowsException(new OperationCanceledException());

            var packageVersionPair = new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.5" };
            var packages = new List<PackageVersionPair>()
            {
                packageVersionPair
            };
            var result = checker.GetInternalPackagesAsync(packages, repositories);
            _loggerMock.Verify(_ => _.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsValueType>(),
                It.IsAny<Exception>(),
                (Func<It.IsValueType, Exception, string>)It.IsAny<object>()), Times.Once);

            repositories = GetInternalRepositoryThrowsException(new AggregateException());
            result = checker.GetInternalPackagesAsync(packages, repositories);

            _loggerMock.Verify(_ => _.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsValueType>(),
                It.IsAny<Exception>(),
                (Func<It.IsValueType, Exception, string>)It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void CompatibilityCheckOfInternalPackageThrowsExceptionDoesNotRecordTargets()
        {
            _internalNuGetCompatibilityHandlerMock.Reset();
            _internalNuGetCompatibilityHandlerMock
                .Setup(checker => checker.CheckCompatibilityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<SourceRepository>>()))
                .Throws(new OperationCanceledException());

            var packageVersionPair = new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.5" };
            var packages = new List<PackageVersionPair>()
            {
                packageVersionPair
            };
            var result = _internalPackagesCompatibilityChecker.Object.CheckAsync(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));

            Task.WaitAll(result.Values.ToArray());
            Assert.AreEqual(0, result.Values.ToList().First().Result.Targets.GetValueOrDefault("netcoreapp3.1").Count);

            _internalNuGetCompatibilityHandlerMock.Reset();
            _internalNuGetCompatibilityHandlerMock
                .Setup(checker => checker.CheckCompatibilityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<SourceRepository>>()))
                .Throws(new AggregateException());

            result = _internalPackagesCompatibilityChecker.Object.CheckAsync(packages, Path.Combine(_testSolutionDirectory, "SolutionWithNugetConfigFile.sln"));

            Task.WaitAll(result.Values.ToArray());
            Assert.AreEqual(0, result.Values.First().Result.Targets.GetValueOrDefault("netcoreapp3.1").Count);
        }
    }
}
