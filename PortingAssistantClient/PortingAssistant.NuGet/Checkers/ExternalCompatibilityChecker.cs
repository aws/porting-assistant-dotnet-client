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

namespace PortingAssistant.NuGet
{
    public class ExternalCompatibilityChecker : ICompatibilityChecker
    {
        private readonly ILogger _logger;
        private readonly IOptions<AnalyzerConfiguration> _options;
        private readonly ITransferUtility _transferUtility;
        private static readonly int _maxProcessConcurrency = 3;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(_maxProcessConcurrency);

        public virtual PackageSourceType CompatibilityCheckerType => PackageSourceType.NUGET;

        public ExternalCompatibilityChecker(
            ITransferUtility transferUtility,
            ILogger<ExternalCompatibilityChecker> logger,
            IOptions<AnalyzerConfiguration> options)
        {
            _logger = logger;
            _options = options;
            _transferUtility = transferUtility;
        }

        public Dictionary<PackageVersionPair, Task<PackageDetails>> CheckAsync(
            IEnumerable<PackageVersionPair> packageVersions,
            string pathToSolution)
        {
            var compatibilityTaskCompletionSources = packageVersions
                .Select(packageVersion =>
                {
                    return new Tuple<PackageVersionPair, TaskCompletionSource<PackageDetails>>(packageVersion, new TaskCompletionSource<PackageDetails>());
                })
                .ToDictionary(t => t.Item1, t => t.Item2);

            _logger.LogInformation("Checking external source for compatibility of {0} package(s)", packageVersions.Count());
            if (packageVersions.Any())
            {
                Task.Run(() =>
                {
                    _semaphore.Wait();
                    try
                    {
                        ProcessCompatibility(packageVersions, compatibilityTaskCompletionSources);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });
            }

            return compatibilityTaskCompletionSources.ToDictionary(t => t.Key, t => t.Value.Task);
        }

        private void ProcessCompatibility(IEnumerable<PackageVersionPair> packageVersions,
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
                    _logger.LogInformation("Downloading {0} from {1} {2}", fileToDownload,
                        _options.Value.DataStoreSettings.S3Endpoint, CompatibilityCheckerType);
                    using var stream = _transferUtility.OpenStream(
                        _options.Value.DataStoreSettings.S3Endpoint, fileToDownload);
                    _logger.LogInformation("Downloading {0} successes", fileToDownload);
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
                    if (ex is AmazonS3Exception && (ex as AmazonS3Exception).StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        var s3Exception = ex as AmazonS3Exception;
                        _logger.LogInformation($"Encountered {s3Exception.GetType()} while downloading and parsing {fileToDownload} " +
                                               $"from {_options.Value.DataStoreSettings.S3Endpoint}, but it was ignored. " +
                                               $"ErrorCode: {s3Exception.ErrorCode}. ErrorMessage: {s3Exception.Message}.");
                    }
                    else
                    {
                        _logger.LogError("Failed when downloading and parsing {0} from {1}, {2}", fileToDownload, _options.Value.DataStoreSettings.S3Endpoint, ex);
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
