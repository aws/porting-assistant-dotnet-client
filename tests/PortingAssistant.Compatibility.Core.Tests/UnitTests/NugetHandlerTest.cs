using System.IO.Compression;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using PortingAssistant.Compatibility.Core.Checkers;
using Assert = NUnit.Framework.Assert;
using Microsoft.Extensions.Logging;

namespace PortingAssistant.Compatibility.Core.Tests.UnitTests
{

    public class NugetHandlerTest
    {
        private Mock<IHttpService> _httpService;
        private Mock<ICompatibilityCheckerNuGetHandler> _compatibilityCheckerNuGetHandler;
        private NugetCompatibilityChecker _nugetCompatibilityChecker;
        private PortabilityAnalyzerCompatibilityChecker _portabilityAnalyzerCompatibilityChecker;
        private SdkCompatibilityChecker _sdkCompatibilityChecker;
        private ILogger<ICompatibilityChecker> _logger;


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
                        },
                        {
                             "net6.0", new SortedSet<string> { "12.0.3", "12.0.4" }
                        }
                    },
                }
            },
            Targets = new Dictionary<string, SortedSet<string>> {
                {
                    "netcoreapp3.1",
                    new SortedSet<string> { "12.0.3", "12.0.4" }
                },
                {
                    "net6.0", new SortedSet<string> { "12.0.3", "12.0.4" }
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


        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _httpService = new Mock<IHttpService>();
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
             _logger = Mock.Of<ILogger<ICompatibilityChecker>>();

            _nugetCompatibilityChecker = new NugetCompatibilityChecker(
                _httpService.Object,
                Mock.Of<ILogger<NugetCompatibilityChecker>>()
                );

            _portabilityAnalyzerCompatibilityChecker = new PortabilityAnalyzerCompatibilityChecker(
                _httpService.Object,
                Mock.Of<ILogger<PortabilityAnalyzerCompatibilityChecker>>()
                );

            _sdkCompatibilityChecker = new SdkCompatibilityChecker(
                _httpService.Object,
                Mock.Of<ILogger<SdkCompatibilityChecker>>()
                );

        }

        private ICompatibilityCheckerNuGetHandler GetExternalNuGetHandler()
        {
            var checkers = new List<ICompatibilityChecker>() { _nugetCompatibilityChecker };
            return new CompatibilityCheckerNuGetHandler(
                checkers.AsEnumerable(),
                Mock.Of<ILogger<CompatibilityCheckerNuGetHandler>>()
                    );
        }

        private ICompatibilityCheckerNuGetHandler GetNamespaceNugetHandler()
        {
            var checkers = new List<ICompatibilityChecker>() { _sdkCompatibilityChecker };
            return new CompatibilityCheckerNuGetHandler(
                checkers.AsEnumerable(),
                Mock.Of<ILogger<CompatibilityCheckerNuGetHandler>>()
                    );
        }

        private ICompatibilityCheckerNuGetHandler GetBothNuGetHandler()
        {
            var checkers = new List<ICompatibilityChecker>() { _nugetCompatibilityChecker, _sdkCompatibilityChecker };
            return new CompatibilityCheckerNuGetHandler(
                    checkers.AsEnumerable(),
                    Mock.Of<ILogger<CompatibilityCheckerNuGetHandler>>()
                    );
        }

        private ICompatibilityCheckerNuGetHandler GetCheckerWithException()
        {
            var checker = new Mock<ICompatibilityChecker>();
            checker.Setup(checker => checker.Check(
                It.IsAny<List<PackageVersionPair>>()
                ))
                .Throws(new Exception("test"));

            var loggerMock = new Mock<ILogger>();

            loggerMock.Reset();

            var checkers = new List<ICompatibilityChecker>() { checker.Object };
            return new CompatibilityCheckerNuGetHandler(
                    checkers.AsEnumerable(),
                    Mock.Of<ILogger<CompatibilityCheckerNuGetHandler>>()
                    );
        }

        private NugetCompatibilityChecker GetExternalPackagesCompatibilityChecker()
        {
            var externalChecker = new NugetCompatibilityChecker(
                _httpService.Object, Mock.Of<ILogger<NugetCompatibilityChecker>>());

            return externalChecker;
        }

        private void SetMockHttpService(PackageDetails packageDetails)
        {
            _httpService.Reset();

            _ = _httpService
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
            var resultTasks = handler.GetNugetPackages( packages);
            Task.WaitAll(resultTasks.Values.ToArray());

            Assert.AreEqual(_packageDetails.Name, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(_packageDetails.Api.Length, resultTasks.Values.First().Result.Api.Length);
            Assert.AreEqual(_packageDetails.Targets.Count, resultTasks.Values.First().Result.Targets.Count);
            Assert.AreEqual(_packageDetails.Versions.Count, resultTasks.Values.First().Result.Versions.Count);
        }

        [Test]
        public void GetPackageWithSdkRepositorySucceeds()
        {
            var handler = GetNamespaceNugetHandler();
            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3", PackageSourceType = PackageSourceType.SDK }
            };
            var resultTasks = handler.GetNugetPackages( packages);
            Task.WaitAll(resultTasks.Values.ToArray());

            Assert.AreEqual(_packageDetails.Name, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(_packageDetails.Api.Length, resultTasks.Values.First().Result.Api.Length);
            Assert.AreEqual(_packageDetails.Targets.Count, resultTasks.Values.First().Result.Targets.Count);
            Assert.AreEqual(_packageDetails.Versions.Count, resultTasks.Values.First().Result.Versions.Count);
        }

        /*
        [Test]
        public void GetPackageWithPortabilityAnalyzerCatalogSucceeds()
        {
            var handler = GetPortabilityAnalzyerHandler();
            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3", PackageSourceType = PackageSourceType.SDK }
            };
            var resultTasks = handler.GetNugetPackages( packages);
            Task.WaitAll(resultTasks.Values.ToArray());

            Assert.AreEqual(_packageDetails.Name, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(_packageDetails.Api.Length, resultTasks.Values.First().Result.Api.Length);
            Assert.AreEqual(_packageDetails.Targets.Count, resultTasks.Values.First().Result.Targets.Count);
            Assert.AreEqual(_packageDetails.Versions.Count, resultTasks.Values.First().Result.Versions.Count);
        }
        */

        [Test]
        public void GetNugetPackagesWithIncompatibleExternalNugetRepositorySucceeds()
        {
            var handler = GetExternalNuGetHandler();
            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.2", PackageSourceType = PackageSourceType.NUGET }
            };
            var resultTasks = handler.GetNugetPackages( packages);
            Task.WaitAll(resultTasks.Values.ToArray());

            Assert.AreEqual(_packageDetails.Name, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(_packageDetails.Api.Length, resultTasks.Values.First().Result.Api.Length);
            Assert.AreEqual(_packageDetails.Targets.Count, resultTasks.Values.First().Result.Targets.Count);
            Assert.AreEqual(_packageDetails.Versions.Count, resultTasks.Values.First().Result.Versions.Count);
        }

        [Test]
        public void HttpServiceOpenStreamCalledOncePerPackage()
        {
            var handler = GetExternalNuGetHandler();

            var packages = new List<PackageVersionPair>() {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3", PackageSourceType = PackageSourceType.NUGET },
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.4" , PackageSourceType = PackageSourceType.NUGET}
            };
            var resultTasks = handler.GetNugetPackages( packages);
            Task.WaitAll(resultTasks.Values.ToArray());
            _httpService.Verify(transfer => transfer.DownloadS3FileAsync(It.IsAny<string>()), Times.Exactly(1));
        }

        [Test]
        public void HttpServiceOpenStreamResultsAreCached()
        {
            var handler = GetExternalNuGetHandler();

            var packages = new List<PackageVersionPair> {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3", PackageSourceType = PackageSourceType.NUGET },
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.4", PackageSourceType = PackageSourceType.NUGET }
            };

            var resultTasks1 = handler.GetNugetPackages( packages);
            var resultTasks2 = handler.GetNugetPackages( packages);
            Task.WaitAll(resultTasks1.Values.ToArray());
            Task.WaitAll(resultTasks2.Values.ToArray());

            var resultTasks = handler.GetNugetPackages( packages);
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
                var resultTasks = externalChecker.Check( packages);
                Task.WaitAll(resultTasks.Result.Values.ToArray());
            });
        }

        [Test]
        public void CompatibilityCheckOfMissingExternalPackageThrowsException()
        {
            SetMockHttpService(new PackageDetails());

            var externalPackagesCompatibilityChecker = GetExternalPackagesCompatibilityChecker();

            var packageVersionPair = new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.5", PackageSourceType = PackageSourceType.NUGET };
            var packages = new List<PackageVersionPair>()
            {
                packageVersionPair
            };

            var resultTasks = externalPackagesCompatibilityChecker.Check( packages);

            Assert.Throws<AggregateException>(() =>
            {
                Task.WaitAll(resultTasks.Result.Values.ToArray());
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
            var resultTasks = handler.GetNugetPackages( packages);
            //MockLogger.Verify(mock => mock.LogError(It.IsAny<string>()), Times.Once);
        }


        [Test]
        public void GetAndCacheNugetPackagesFromS3Succeeds()
        {
            var handler = GetExternalNuGetHandler();
            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3", PackageSourceType = PackageSourceType.NUGET }
            };
            var resultTasks = handler.GetNugetPackages( packages);
            Task.WaitAll(resultTasks.Values.ToArray());

            Assert.AreEqual(_packageDetails.Name, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(_packageDetails.Api.Length, resultTasks.Values.First().Result.Api.Length);
            Assert.AreEqual(_packageDetails.Targets.Count, resultTasks.Values.First().Result.Targets.Count);
            Assert.AreEqual(_packageDetails.Versions.Count, resultTasks.Values.First().Result.Versions.Count);
        }
    }
}
