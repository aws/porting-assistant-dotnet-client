using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.NuGet.Interfaces;
using PortingAssistant.Client.NuGet.Utils;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace PortingAssistant.Client.NuGet
{
    public class ExternalCompatibilityChecker : ICompatibilityChecker
    {
        private readonly ILogger _logger;
        private readonly IHttpService _httpService;
        private readonly IFileSystem _fileSystem;
        private static readonly int _maxProcessConcurrency = 3;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(_maxProcessConcurrency);

        public virtual PackageSourceType CompatibilityCheckerType => PackageSourceType.NUGET;

        public ExternalCompatibilityChecker(
            IHttpService httpService,
            ILogger<ExternalCompatibilityChecker> logger,
            IFileSystem fileSystem = null)
        {
            _logger = logger;
            _httpService = httpService;
            if (fileSystem != null)
                _fileSystem = fileSystem;
            else
                _fileSystem = new FileSystem();
        }

        public Dictionary<PackageVersionPair, Task<PackageDetails>> Check(
            IEnumerable<PackageVersionPair> packageVersions,
            string pathToSolution, bool isIncremental = false, bool refresh = false)
        {
            var packagesToCheck = packageVersions;

            if (CompatibilityCheckerType == PackageSourceType.SDK)
            {
                packagesToCheck = packageVersions.Where(package => package.PackageSourceType == PackageSourceType.SDK);
            }

            var compatibilityTaskCompletionSources = packagesToCheck
                .Select(packageVersion =>
                {
                    return new Tuple<PackageVersionPair, TaskCompletionSource<PackageDetails>>(packageVersion, new TaskCompletionSource<PackageDetails>());
                })
                .ToDictionary(t => t.Item1, t => t.Item2);

            _logger.LogInformation("Checking {0} for compatibility of {1} package(s)", CompatibilityCheckerType, packagesToCheck.Count());
            if (packagesToCheck.Any())
            {
                Task.Run(() =>
                {
                    _semaphore.Wait();
                    try
                    {
                        ProcessCompatibility(packagesToCheck, compatibilityTaskCompletionSources, pathToSolution, isIncremental, refresh);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });
            }

            return compatibilityTaskCompletionSources.ToDictionary(t => t.Key, t => t.Value.Task);
        }

        private async void ProcessCompatibility(IEnumerable<PackageVersionPair> packageVersions,
            Dictionary<PackageVersionPair, TaskCompletionSource<PackageDetails>> compatibilityTaskCompletionSources,
            string pathToSolution, bool isIncremental, bool incrementalRefresh)
        {
            var packageVersionsFound = new HashSet<PackageVersionPair>();
            var packageVersionsWithErrors = new HashSet<PackageVersionPair>();

            var packageVersionsGroupedByPackageId = packageVersions
                .GroupBy(pv => pv.PackageId)
                .ToDictionary(pvGroup => pvGroup.Key, pvGroup => pvGroup.ToList());

            foreach (var groupedPackageVersions in packageVersionsGroupedByPackageId)
            {
                var packageToDownload = groupedPackageVersions.Key.ToLower();
                var fileToDownload = GetDownloadFilePath(CompatibilityCheckerType, packageToDownload);

                try
                {
                    string tempDirectoryPath = GetTempDirectory(pathToSolution);
                    PackageDetails packageDetails = null;

                    if (isIncremental)
                    {
                        if (incrementalRefresh || !IsPackageInFile(fileToDownload, tempDirectoryPath))
                        {
                            _logger.LogInformation("Downloading {0} from {1}", fileToDownload, CompatibilityCheckerType);
                            packageDetails = await GetPackageDetailFromS3(fileToDownload, _httpService);
                            _logger.LogInformation("Caching {0} from {1} to Temp", fileToDownload, CompatibilityCheckerType);
                            CachePackageDetailsToFile(fileToDownload, packageDetails, tempDirectoryPath);
                        }
                        else
                        {
                            _logger.LogInformation("Fetching {0} from {1} from Temp", fileToDownload, CompatibilityCheckerType);
                            packageDetails = GetPackageDetailFromFile(fileToDownload, tempDirectoryPath);
                        }
                    }
                    else
                        packageDetails = await GetPackageDetailFromS3(fileToDownload, _httpService);

                    if (packageDetails.Name == null || !string.Equals(packageDetails.Name.Trim(),
                        packageToDownload.Trim(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        throw new PackageDownloadMismatchException(
                            actualPackage: packageDetails.Name,
                            expectedPackage: packageToDownload);
                    }

                    foreach (var packageVersion in groupedPackageVersions.Value)
                    {
                        if (compatibilityTaskCompletionSources.TryGetValue(packageVersion, out var taskCompletionSource))
                        {
                            taskCompletionSource.SetResult(packageDetails);
                            packageVersionsFound.Add(packageVersion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404"))
                    {
                        _logger.LogInformation($"Encountered {ex.GetType()} while downloading and parsing {fileToDownload} " +
                                               $"from {CompatibilityCheckerType}, but it was ignored. " +
                                               $"ErrorMessage: {ex.Message}.");
                        // filter all 404 errors
                        ex = null;
                    }
                    else
                    {
                        _logger.LogError("Failed when downloading and parsing {0} from {1}, {2}", fileToDownload, CompatibilityCheckerType, ex);
                    }

                    foreach (var packageVersion in groupedPackageVersions.Value)
                    {
                        if (compatibilityTaskCompletionSources.TryGetValue(packageVersion, out var taskCompletionSource))
                        {
                            taskCompletionSource.SetException(new PortingAssistantClientException(ExceptionMessage.PackageNotFound(packageVersion), ex));
                            packageVersionsWithErrors.Add(packageVersion);
                        }
                    }
                }
            }

            foreach (var packageVersion in packageVersions)
            {
                if (packageVersionsFound.Contains(packageVersion) || packageVersionsWithErrors.Contains(packageVersion))
                {
                    continue;
                }

                if (compatibilityTaskCompletionSources.TryGetValue(packageVersion, out var taskCompletionSource))
                {
                    var errorMessage = $"Could not find package {packageVersion} in external source; try checking an internal source.";
                    _logger.LogInformation(errorMessage);

                    var innerException = new PackageNotFoundException(errorMessage);
                    taskCompletionSource.TrySetException(new PortingAssistantClientException(ExceptionMessage.PackageNotFound(packageVersion), innerException));
                }
            }
        }

        private string GetDownloadFilePath(PackageSourceType CompatibilityCheckerType, string packageToDownload)
        {
            var fileToDownload = $"{packageToDownload}.json.gz";
            var downloadFilePath = fileToDownload;
            switch (CompatibilityCheckerType)
            {
                case PackageSourceType.NUGET:
                    break;
                case PackageSourceType.SDK:
                    downloadFilePath = Path.Combine("namespaces", fileToDownload);
                    break;
                default:
                    break;
            }

            return downloadFilePath;
        }

        public class PackageFromS3
        {
            public PackageDetails Package { get; set; }
            public PackageDetails Namespaces { get; set; }
        }

        public string GetTempDirectory(string pathToSolution)
        {
            if (pathToSolution != null)
            {
                string solutionId;
                using (var sha = new SHA256Managed())
                {
                    byte[] textData = System.Text.Encoding.UTF8.GetBytes(pathToSolution);
                    byte[] hash = sha.ComputeHash(textData);
                    solutionId = BitConverter.ToString(hash);
                }
                var tempSolutionDirectory = Path.Combine(_fileSystem.GetTempPath(), solutionId);
                tempSolutionDirectory = tempSolutionDirectory.Replace("-", "");
                return tempSolutionDirectory;
            }
            return null;
        }
        public bool IsPackageInFile(string fileToDownload, string _tempSolutionDirectory)
        {
            string filePath = Path.Combine(_tempSolutionDirectory, fileToDownload);
            return _fileSystem.FileExists(filePath);
        }
        public async Task<PackageDetails> GetPackageDetailFromS3(string fileToDownload, IHttpService httpService)
        {
            using var stream = await httpService.DownloadS3FileAsync(fileToDownload);
            using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            using var streamReader = new StreamReader(gzipStream);
            var data = JsonConvert.DeserializeObject<PackageFromS3>(streamReader.ReadToEnd());
            var packageDetails = data.Package ?? data.Namespaces;
            return packageDetails;
        }
        public async void CachePackageDetailsToFile(string fileName, PackageDetails packageDetail, string _tempSolutionDirectory)
        {
            if (!_fileSystem.DirectoryExists(_tempSolutionDirectory))
            {
                _fileSystem.CreateDirectory(_tempSolutionDirectory);
            }
            string filePath = Path.Combine(_tempSolutionDirectory, fileName);
            var data = JsonConvert.SerializeObject(packageDetail);
            using Stream compressedFileStream = _fileSystem.FileOpenWrite(filePath);
            using var gzipStream = new GZipStream(compressedFileStream, CompressionMode.Compress);
            using var streamWriter = new StreamWriter(gzipStream);
            await streamWriter.WriteAsync(data);
        }
        public PackageDetails GetPackageDetailFromFile(string fileToDownload, string _tempSolutionDirectory)
        {
            string filePath = Path.Combine(_tempSolutionDirectory, fileToDownload);
            using var compressedFileStream = _fileSystem.FileOpenRead(filePath);
            using var gzipStream = new GZipStream(compressedFileStream, CompressionMode.Decompress);
            using var streamReader = new StreamReader(gzipStream);
            var data = JsonConvert.DeserializeObject<PackageDetails>(streamReader.ReadToEnd());
            return data;
        }
    }
}
