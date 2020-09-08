using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PortingAssistant.NuGet;
using PortingAssistant.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PortingAssistant.NuGet.InternalNuGetChecker;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.IO;
using Amazon.S3.Transfer;
using Newtonsoft.Json;
using System.IO.Compression;

namespace PortingAssistantNuGetTest
{
    public class PortingAssistantNuGetHandlerTest
    {
        private Mock<ITransferUtility> _transferUtilityMock;
        private Mock<IPortingAssistantInternalNuGetCompatibilityHandler> _checkComptHanderMock;
        private Mock<InternalPackagesCompatibilityChecker> _internalChecker;
        private ExternalPackagesCompatibilityChecker _externalChecker;
        private Mock<ILogger<PortingAssistantNuGetHandler>> _logger;
        private readonly string _testSolutionFolderPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
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

        private async Task<InternalNuGetCompatibilityResult> getCompatibilityResult(
            int timeout, bool compatility)
        {
            await Task.Delay(timeout);

            return new InternalNuGetCompatibilityResult
            {
                CompatibleDlls = null,
                IsCompatible = compatility,
                IncompatibleDlls = null,
                source = "nuget.woot.com",
                DepedencyPackages = null
            };
        }

        private IEnumerable<SourceRepository> getInternalRepository()
        {
            var mockRepostiories = new List<SourceRepository>();
            var mockSourceRepository = new Mock<SourceRepository>();
            var mockSource = new Mock<FindPackageByIdResource>();

            mockSource.Reset();
            mockSource.Setup(source => source.DoesPackageExistAsync(
                It.IsAny<string>(),
                It.IsAny<NuGetVersion>(),
                It.IsAny<SourceCacheContext>(),
                It.IsAny<NuGet.Common.ILogger>(),
                It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(5);
                    return true;
                });

            mockSourceRepository.Reset();
            mockSourceRepository.Setup(source => source.GetResourceAsync<
                FindPackageByIdResource>())
                .Returns(async () =>
               {
                   await Task.Delay(5);
                   return mockSource.Object;
               });

            mockRepostiories.Add(mockSourceRepository.Object);
            return mockRepostiories.AsEnumerable();
        }

        private IEnumerable<SourceRepository> getInternalRepositoryNotExist()
        {
            var mockRepostiories = new List<SourceRepository>();
            var mockSourceRepository = new Mock<SourceRepository>();
            var mockSource = new Mock<FindPackageByIdResource>();

            mockSource.Reset();
            mockSource.Setup(source => source.DoesPackageExistAsync(
                It.IsAny<string>(),
                It.IsAny<NuGetVersion>(),
                It.IsAny<SourceCacheContext>(),
                It.IsAny<NuGet.Common.ILogger>(),
                It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(5);
                    return false;
                });

            mockSourceRepository.Reset();
            mockSourceRepository.Setup(source => source.GetResourceAsync<
                FindPackageByIdResource>())
                .Returns(async () =>
                {
                    await Task.Delay(5);
                    return mockSource.Object;
                });

            mockRepostiories.Add(mockSourceRepository.Object);
            return mockRepostiories.AsEnumerable();
        }

        private IEnumerable<SourceRepository> getInternalRepositoryThrowException(Exception exception)
        {
            var mockRepostiories = new List<SourceRepository>();
            var mockSourceRepository = new Mock<SourceRepository>();
            var mockSource = new Mock<FindPackageByIdResource>();

            mockSource.Reset();
            mockSource.Setup(source => source.DoesPackageExistAsync(
                It.IsAny<string>(),
                It.IsAny<NuGetVersion>(),
                It.IsAny<SourceCacheContext>(),
                It.IsAny<NuGet.Common.ILogger>(),
                It.IsAny<CancellationToken>()))
                .Throws(exception);

            mockSourceRepository.Reset();
            mockSourceRepository.Setup(source => source.GetResourceAsync<
                FindPackageByIdResource>())
                .Returns(async () =>
                {
                    await Task.Delay(5);
                    return mockSource.Object;
                });

            mockRepostiories.Add(mockSourceRepository.Object);
            return mockRepostiories.AsEnumerable();
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _transferUtilityMock = new Mock<ITransferUtility>();
            _checkComptHanderMock = new Mock<IPortingAssistantInternalNuGetCompatibilityHandler>();
        }

        [SetUp]
        public void Setup()
        {
            _transferUtilityMock.Reset();
            _transferUtilityMock
                .Setup(transfer => transfer.OpenStream(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string bucket, string key) =>
                {
                    var stream = new MemoryStream();
                    var writer = new StreamWriter(stream);
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
                });

            _checkComptHanderMock.Reset();

            _checkComptHanderMock
                .Setup(checker => checker.CheckCompatibilityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<SourceRepository>>()))
                .Returns((string packageName, string version, string targetFramework, IEnumerable<SourceRepository> sourceRepositories) =>
                {
                    return getCompatibilityResult(200, true);
                });


            _externalChecker = new ExternalPackagesCompatibilityChecker(
                _transferUtilityMock.Object,
                NullLogger<ExternalPackagesCompatibilityChecker>.Instance,
                Options.Create(new AnalyzerConfiguration {
                    DataStoreSettings = new DataStoreSettings
                    {
                        S3Endpoint = "Bucket"
                    }
                })
                );

            _internalChecker = new Mock<InternalPackagesCompatibilityChecker>(
                _checkComptHanderMock.Object,
                NullLogger<InternalPackagesCompatibilityChecker>.Instance
                );


            _internalChecker.Reset();
            _internalChecker.Setup(checker => checker.GetInternalRepository(
                It.IsAny<string>())).Returns(() =>
                {
                    return getInternalRepository();
                });
        }

        private IPortingAssistantNuGetHandler GetExternalNuGetHandler()
        {
            var checkers = new List<ICompatibilityChecker>() { _externalChecker };
            return new PortingAssistantNuGetHandler(
                    NullLogger<PortingAssistantNuGetHandler>.Instance,
                    checkers.AsEnumerable()
                    );
        }

        private IPortingAssistantNuGetHandler GetInternalNuGetHandler()
        {
            var checkers = new List<ICompatibilityChecker>() { _internalChecker.Object };
            return new PortingAssistantNuGetHandler(
                    NullLogger<PortingAssistantNuGetHandler>.Instance,
                    checkers.AsEnumerable()
                    );
        }

        private IPortingAssistantNuGetHandler GetBothNuGetHandler()
        {
            var checkers = new List<ICompatibilityChecker>() { _externalChecker, _internalChecker.Object };
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

            _logger = new Mock<ILogger<PortingAssistantNuGetHandler>>();

            _logger.Reset();

            _logger.Setup(_ => _.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsValueType>(),
                It.IsAny<Exception>(),
                (Func<It.IsValueType, Exception, string>)It.IsAny<object>()));

            var checkers = new List<ICompatibilityChecker>() { checker.Object };
            return new PortingAssistantNuGetHandler(
                    _logger.Object,
                    checkers.AsEnumerable()
                    );
        }

        private ExternalPackagesCompatibilityChecker GetExternalPackagesCompatibilityChecker()
        {
            var externalchecker = new ExternalPackagesCompatibilityChecker(
                _transferUtilityMock.Object,
                NullLogger<ExternalPackagesCompatibilityChecker>.Instance,
                Options.Create(new AnalyzerConfiguration
                {
                    DataStoreSettings = new DataStoreSettings
                    {
                        S3Endpoint = "Bucket"
                    }
                })
            );

            return externalchecker;
        }

        private void setMockTransferUtility(PackageDetails packageDetails)
        {
            _transferUtilityMock.Reset();
            _transferUtilityMock
                .Setup(transfer => transfer.OpenStream(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string bucket, string key) =>
                {
                    var stream = new MemoryStream();
                    var writer = new StreamWriter(stream);
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
                });
        }

        [Test]
        public void TestCompatibleExternalSource()
        {
            var handler = GetExternalNuGetHandler();
            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3" }
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());
            Assert.AreEqual(_packageDetails.Name, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(_packageDetails.Api.Count(), resultTasks.Values.First().Result.Api.Count());
            Assert.AreEqual(_packageDetails.Targets.Count(), resultTasks.Values.First().Result.Targets.Count());
            Assert.AreEqual(_packageDetails.Versions.Count(), resultTasks.Values.First().Result.Versions.Count());
        }

        [Test]
        public void TestCompatibleInternalSource()
        {
            var handler = GetInternalNuGetHandler();

            _checkComptHanderMock.Reset();

            _checkComptHanderMock
                .Setup(checker => checker.CheckCompatibilityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<SourceRepository>>()))
                .Returns((string packageName, string version, string targetFramework, IEnumerable<SourceRepository> sourceRepositories) =>
                {
                    return getCompatibilityResult(1, true);
                });

            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3" }
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());
            Assert.AreEqual(packages.First().PackageId, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(packages.First().Version, resultTasks.Values.First().Result.Targets["netcoreapp3.1"].First());
        }


        [Test]
        public void TestNotCompatibleExternalSource()
        {
            var handler = GetExternalNuGetHandler();

            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.2" }
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());
            Assert.AreEqual(_packageDetails.Name, resultTasks.Values.First().Result.Name);
            Assert.AreEqual(_packageDetails.Api.Count(), resultTasks.Values.First().Result.Api.Count());
            Assert.AreEqual(_packageDetails.Targets.Count(), resultTasks.Values.First().Result.Targets.Count());
            Assert.AreEqual(_packageDetails.Versions.Count(), resultTasks.Values.First().Result.Versions.Count());
        }

        [Test]
        public void TestNotCompatibleInternalSource()
        {
            var handler = GetInternalNuGetHandler();

            _checkComptHanderMock.Reset();

            _checkComptHanderMock
                .Setup(checker => checker.CheckCompatibilityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<SourceRepository>>()))
                .Returns((string packageName, string version, string targetFramework, IEnumerable<SourceRepository> sourceRepositories) =>
                {
                    return getCompatibilityResult(1, false);
                });

            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.2" }
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());

            Assert.AreEqual(packages.First().PackageId, resultTasks.Values.First().Result.Name);
            Assert.AreEqual("netcoreapp3.1", resultTasks.Values.First().Result.Targets.First().Key);
            Assert.AreEqual(0, resultTasks.Values.First().Result.Targets["netcoreapp3.1"].Count);
        }

        [Test]
        public void TestNotExists()
        {

            _internalChecker.Reset();
            _internalChecker.Setup(checker => checker.GetInternalRepository(
                It.IsAny<string>())).Returns(() =>
                {
                    return getInternalRepositoryNotExist();
                });

            _transferUtilityMock.Reset();
            _transferUtilityMock
                .Setup(transfer => transfer.OpenStream(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception());

            var handler = GetBothNuGetHandler();

            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.5" }
            };

            Assert.Throws<AggregateException>(() =>
            {
                var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));
                Task.WaitAll(resultTasks.Values.ToArray());
            });
        }

        [Test]
        public void TestNotExistsNuGet()
        {
            _internalChecker.Reset();
            _internalChecker.Setup(checker => checker.GetInternalRepository(
                It.IsAny<string>())).Returns(() =>
                {
                    return getInternalRepositoryNotExist();
                });

            _transferUtilityMock.Reset();
            _transferUtilityMock
                .Setup(transfer => transfer.OpenStream(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception());

            var handler = GetBothNuGetHandler();

            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.5" },
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.6" }
            };

            try
            {
                var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));
                Task.WaitAll(resultTasks.Values.ToArray());
            }
            catch (Exception)
            { }

            try
            {
                var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));
                Task.WaitAll(resultTasks.Values.ToArray());
            }
            catch (Exception)
            { }

            _transferUtilityMock.Verify(transfer => transfer.OpenStream(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void TestMultiple()
        {
            var handler = GetExternalNuGetHandler();

            var packages = new List<PackageVersionPair>() {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3" },
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.4" }
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());
            _transferUtilityMock.Verify(transfer => transfer.OpenStream(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
        }

        [Test]
        public void TestNuGet()
        {
            var handler = GetExternalNuGetHandler();

            var packages = new List<PackageVersionPair>() {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3" },
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.4" }
            };

            var resultTasks1 = handler.GetNugetPackages(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));
            var resultTasks2 = handler.GetNugetPackages(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));
            Task.WaitAll(resultTasks1.Values.ToArray());
            Task.WaitAll(resultTasks2.Values.ToArray());

            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));
            Task.WaitAll(resultTasks.Values.ToArray());

            // Doesn't fire another request when requesting for same package.
            _transferUtilityMock.Verify(transfer => transfer.OpenStream(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
        }

        [Test]
        public void TestInvalidJsonResponse()
        {
            _transferUtilityMock.Reset();
            _transferUtilityMock
                .Setup(transfer => transfer.OpenStream(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string bucket, string key) =>
                {
                    MemoryStream stream = new MemoryStream();
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write("INVALID");
                    writer.Flush();
                    writer.BaseStream.Position = 0;
                    return writer.BaseStream;
                });

            var externalchecker = GetExternalPackagesCompatibilityChecker();

            var packageVersionPair = new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.5" };
            var packages = new List<PackageVersionPair>()
            {
                packageVersionPair
            };
            Assert.Throws<AggregateException>(() =>
            {
                var resultTasks = externalchecker.CheckAsync(packages, null);
                Task.WaitAll(resultTasks.Values.ToArray());
            });
        }

        [Test]
        public void TestNotExistInExternalCheck()
        {
            setMockTransferUtility(new PackageDetails());

            var externalchecker = GetExternalPackagesCompatibilityChecker();

            var packageVersionPair = new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.5" };
            var packages = new List<PackageVersionPair>()
            {
                packageVersionPair
            };

            var resultTasks = externalchecker.CheckAsync(packages, null);

            _logger.Verify(_ => _.Log(
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
        public void TestCheckerException()
        {

            var handler = GetCheckerWithException();
            var packages = new List<PackageVersionPair>()
            {
              new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.3" }
            };
            var resultTasks = handler.GetNugetPackages(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));
            _logger.Verify(_ => _.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsValueType>(),
                It.IsAny<Exception>(),
                (Func<It.IsValueType, Exception, string>)It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void TestGetInternalRepository()
        {
            var checker = new InternalPackagesCompatibilityChecker(
                _checkComptHanderMock.Object,
                NullLogger<InternalPackagesCompatibilityChecker>.Instance
                );

            var result = checker.GetInternalRepository(Path.Combine(_testSolutionFolderPath, "TestSolution.sln")).ToList();

            Assert.AreEqual("nuget.woot.com", result.First().PackageSource.Name.ToLower());

        }

        [Test]
        public void TestInternalPackagesThrowException()
        {
            var checker = _internalChecker.Object;
            var repositories = getInternalRepositoryThrowException(new OperationCanceledException());

            var packageVersionPair = new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.5" };
            var packages = new List<PackageVersionPair>()
            {
                packageVersionPair
            };
            var result = checker.getInternalPackagesAsync(Path.Combine(_testSolutionFolderPath, "TestSolution.sln"), packages, repositories);
            _logger.Verify(_ => _.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsValueType>(),
                It.IsAny<Exception>(),
                (Func<It.IsValueType, Exception, string>)It.IsAny<object>()), Times.Once);

            repositories = getInternalRepositoryThrowException(new AggregateException());
            result = checker.getInternalPackagesAsync(Path.Combine(_testSolutionFolderPath, "TestSolution.sln"), packages, repositories);
            _logger.Verify(_ => _.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsValueType>(),
                It.IsAny<Exception>(),
                (Func<It.IsValueType, Exception, string>)It.IsAny<object>()), Times.Once);

        }

        [Test]
        public void TestInternalCheckCompatibilityThrowException()
        {
            _checkComptHanderMock.Reset();
            _checkComptHanderMock
                .Setup(checker => checker.CheckCompatibilityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<SourceRepository>>()))
                .Throws(new OperationCanceledException());

            var repositories = getInternalRepositoryThrowException(new OperationCanceledException());

            var packageVersionPair = new PackageVersionPair { PackageId = "Newtonsoft.Json", Version = "12.0.5" };
            var packages = new List<PackageVersionPair>()
            {
                packageVersionPair
            };
            var result = _internalChecker.Object.CheckAsync(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));

            Task.WaitAll(result.Values.ToArray());
            Assert.AreEqual(0, result.Values.ToList().First().Result.Targets.GetValueOrDefault("netcoreapp3.1").Count);

            _checkComptHanderMock.Reset();
            _checkComptHanderMock
                .Setup(checker => checker.CheckCompatibilityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<SourceRepository>>()))
                .Throws(new AggregateException());

            result = _internalChecker.Object.CheckAsync(packages, Path.Combine(_testSolutionFolderPath, "TestSolution.sln"));

            Task.WaitAll(result.Values.ToArray());
            Assert.AreEqual(0, result.Values.ToList().First().Result.Targets.GetValueOrDefault("netcoreapp3.1").Count);
        }
    }
}
