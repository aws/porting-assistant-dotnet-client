using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Amazon.S3.Transfer;
using System.IO;
using System.IO.Compression;
using Amazon.S3;
using PortingAssistant.Model;
using PortingAssistant.NuGet.Interfaces;

namespace PortingAssistant.NuGet
{
    public class ExternalCompatibilityChecker : ICompatibilityChecker
    {
        private readonly ILogger _logger;
        private readonly IHttpService _httpService;
        private static readonly int _maxProcessConcurrency = 3;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(_maxProcessConcurrency);

        public virtual PackageSourceType CompatibilityCheckerType => PackageSourceType.NUGET;

        public ExternalCompatibilityChecker(
            IHttpService httpService,
            ILogger<ExternalCompatibilityChecker> logger)
        {
            _logger = logger;
            _httpService = httpService;
        }

        public Dictionary<PackageVersionPair, Task<PackageDetails>> CheckAsync(
            IEnumerable<PackageVersionPair> packageVersions,
            string pathToSolution)
        {
            var packagesToCheck = packageVersions;

            if(CompatibilityCheckerType == PackageSourceType.SDK)
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
                        ProcessCompatibility(packagesToCheck, compatibilityTaskCompletionSources);
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
            Dictionary<PackageVersionPair, TaskCompletionSource<PackageDetails>> compatibilityTaskCompletionSources)
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
                    _logger.LogInformation("Downloading {0} from {1}", fileToDownload,
                        CompatibilityCheckerType);
                    using var stream = await _httpService.DownloadS3FileAsync(fileToDownload);
                    using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
                    using var streamReader = new StreamReader(gzipStream);
                    var data = JsonConvert.DeserializeObject<PackageFromS3>(streamReader.ReadToEnd());
                    var packageDetails = data.Package ?? data.Namespaces;

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
                    downloadFilePath = "namespaces/" + fileToDownload;
                    break;
                default:
                    break;
            }

            return downloadFilePath;
        }

        private class PackageFromS3
        {
            public PackageDetails Package { get; set; }
            public PackageDetails Namespaces { get; set; }
        }
    }
}
