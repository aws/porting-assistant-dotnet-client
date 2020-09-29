﻿using System;
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
using PortingAssistant.Model;
using Newtonsoft.Json.Linq;
using Amazon.S3;

namespace PortingAssistant.NuGet
{
    /// <summary>
    /// Compatibility checker for Portability Analyzer results
    /// </summary>
    public class PortabilityAnalyzerCompatibilityChecker : ICompatibilityChecker
    {
        private const string NamespaceLookupFile = "microsoftlibs.namespace.lookup.json";
        private readonly ILogger _logger;
        private readonly IOptions<AnalyzerConfiguration> _options;
        private readonly ITransferUtility _transferUtility;
        private Task<Dictionary<string, string>> _manifest;
        private static readonly int _maxProcessConcurrency = 3;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(_maxProcessConcurrency);

        public PackageSourceType CompatibilityCheckerType => PackageSourceType.PORTABILITY_ANALYZER;

        /// <summary>
        /// Creates a new instance of Portability Analyzer compatibility checker
        /// </summary>
        /// <param name="transferUtility">The transferUtility object to read data from S3</param>
        /// <param name="logger">Logger object</param>
        /// <param name="options">Options used for accessing the Portability Analyzer results</param>
        public PortabilityAnalyzerCompatibilityChecker(
            ITransferUtility transferUtility,
            ILogger<ExternalPackagesCompatibilityChecker> logger,
            IOptions<AnalyzerConfiguration> options)
        {
            _logger = logger;
            _options = options;
            _transferUtility = transferUtility;
            _manifest = null;
        }

        /// <summary>
        /// Checks the packages in Portability Analyzer
        /// </summary>
        /// <param name="packageVersions">The package versions to check</param>
        /// <param name="pathToSolution">Path to the solution to check</param>
        /// <returns>The results of the compatibility check</returns>
        public Dictionary<PackageVersionPair, Task<PackageDetails>> CheckAsync(
            IEnumerable<PackageVersionPair> packageVersions,
            string pathToSolution)
        {
            var compatibilityTaskCompletionSources = new Dictionary<PackageVersionPair, TaskCompletionSource<PackageDetails>>();

            try
            {
                if (_manifest == null)
                {
                    _manifest = GetManifestAsync();
                }
                Task.WaitAll(_manifest);
                var manifest = _manifest.Result;

                var foundPackages = new Dictionary<string, List<PackageVersionPair>>();
                packageVersions.ToList().ForEach(p =>
                {
                    if (p.PackageSourceType != PackageSourceType.SDK)
                    {
                        return;
                    }

                    var value = manifest.GetValueOrDefault(p.PackageId.ToLower(), null);
                    if (value != null)
                    {
                        compatibilityTaskCompletionSources.Add(p, new TaskCompletionSource<PackageDetails>());
                        if (!foundPackages.ContainsKey(value))
                        {
                            foundPackages.Add(value, new List<PackageVersionPair>());
                        }
                        foundPackages[value].Add(p);
                    }
                });

                _logger.LogInformation("Checking Portability Analyzer source for compatibility of {0} package(s)", foundPackages.Count);
                if (foundPackages.Any())
                {
                    Task.Run(() =>
                    {
                        _semaphore.Wait();
                        try
                        {
                            ProcessCompatibility(packageVersions, foundPackages, compatibilityTaskCompletionSources);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    });
                }

                return compatibilityTaskCompletionSources.ToDictionary(t => t.Key, t => t.Value.Task);
            }
            catch (Exception ex)
            {
                foreach (var packageVersion in packageVersions)
                {
                    if (compatibilityTaskCompletionSources.TryGetValue(packageVersion, out var taskCompletionSource))
                    {
                        taskCompletionSource.TrySetException(
                            new PortingAssistantClientException(ExceptionMessage.PackageNotFound(packageVersion), ex));
                    }
                }
                return compatibilityTaskCompletionSources.ToDictionary(t => t.Key, t => t.Value.Task);
            }
        }

        /// <summary>
        /// Processes the package compatibility
        /// </summary>
        /// <param name="packageVersions">Collection of package versions to check</param>
        /// <param name="foundPackages">Collection of packages found</param>
        /// <param name="compatibilityTaskCompletionSources">The results of the compatibility check to process</param>
        private void ProcessCompatibility(IEnumerable<PackageVersionPair> packageVersions,
            Dictionary<string, List<PackageVersionPair>> foundPackages,
            Dictionary<PackageVersionPair, TaskCompletionSource<PackageDetails>> compatibilityTaskCompletionSources)
        {
            var packageVersionsFound = new HashSet<PackageVersionPair>();
            var packageVersionsWithErrors = new HashSet<PackageVersionPair>();

            foreach (var url in foundPackages)
            {
                try
                {
                    _logger.LogInformation("Downloading {0} from {1} {2}", url.Key, _options.Value.DataStoreSettings.S3Endpoint, CompatibilityCheckerType);
                    using var stream = _transferUtility.OpenStream(
                        _options.Value.DataStoreSettings.S3Endpoint, url.Key.Substring(_options.Value.DataStoreSettings.S3Endpoint.Length + 6));
                    _logger.LogInformation("Downloading {0} successes", url.Key);
                    using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
                    using var streamReader = new StreamReader(gzipStream);
                    var packageFromS3 = JsonConvert.DeserializeObject<PackageFromS3>(streamReader.ReadToEnd());
                    packageFromS3.Package.Name = url.Value.First().PackageId;
                    foreach (var packageVersion in url.Value)
                    {
                        if (compatibilityTaskCompletionSources.TryGetValue(packageVersion, out var taskCompletionSource))
                        {
                            taskCompletionSource.SetResult(packageFromS3.Package);
                            packageVersionsFound.Add(packageVersion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is AmazonS3Exception && (ex as AmazonS3Exception).StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        var s3Exception = ex as AmazonS3Exception;
                        _logger.LogInformation($"Encountered {s3Exception.GetType()} while downloading and parsing {url.Key} " +
                                               $"from {_options.Value.DataStoreSettings.S3Endpoint}, but it was ignored. " +
                                               $"ErrorCode: {s3Exception.ErrorCode}. ErrorMessage: {s3Exception.Message}.");
                    }
                    else
                    {
                        _logger.LogError("Failed when downloading and parsing {0} from {1}, {2}", url.Key, _options.Value.DataStoreSettings.S3Endpoint.Length, ex);
                    }

                    foreach (var packageVersion in url.Value)
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

        private async Task<Dictionary<string, string>> GetManifestAsync()
        {
            using var stream = await _transferUtility.OpenStreamAsync(
                    _options.Value.DataStoreSettings.S3Endpoint, "microsoftlibs.namespace.lookup.json");
            using var streamReader = new StreamReader(stream);
            return JsonConvert.DeserializeObject<JObject>(streamReader.ReadToEnd()).ToObject<Dictionary<string, string>>();
        }

        private class PackageFromS3
        {
            public PackageDetails Package { get; set; }
        }
    }
}