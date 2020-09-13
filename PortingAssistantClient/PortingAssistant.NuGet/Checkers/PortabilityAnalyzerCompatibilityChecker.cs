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
using PortingAssistant.Model;

namespace PortingAssistant.NuGet
{
    /// <summary>
    /// Compatiblity checker for Portability Analyzer results
    /// </summary>
    public class PortabilityAnalyzerCompatibilityChecker : ICompatibilityChecker
    {
        private readonly ILogger _logger;
        private readonly IOptions<AnalyzerConfiguration> _options;
        private readonly ITransferUtility _transferUtility;
        private static readonly int _maxProcessConcurrency = 3;
        private static readonly SemaphoreSlim _processConcurrency = new SemaphoreSlim(_maxProcessConcurrency);

        /// <summary>
        /// Creates a new instance of Portability Analyzer compatiblity checker
        /// </summary>
        /// <param name="transferUtility">The transferUtility object to read data from S3</param>
        /// <param name="logger">Logger object</param>
        /// <param name="options">Options used for accessing the Portability Analyzer results</param>
        public PortabilityAnalyzerCompatibilityChecker(
            ITransferUtility transferUtility,
            ILogger<ExternalPackagesCompatibilityChecker> logger,
            IOptions<AnalyzerConfiguration> options
            )
        {
            _logger = logger;
            _options = options;
            _transferUtility = transferUtility;
        }

        /// <summary>
        /// Checks the packages in Portability Analyzer
        /// </summary>
        /// <param name="packageVersions">The package versions to check</param>
        /// <param name="pathToSolution">Path to the solution to check</param>
        /// <returns>The results of the compatibility check</returns>
        public Dictionary<PackageVersionPair, Task<PackageDetails>> CheckAsync(
            List<PackageVersionPair> packageVersions,
            string pathToSolution
            )
        {
            var resultsDict = new Dictionary<PackageVersionPair, TaskCompletionSource<PackageDetails>>();

            try
            {
                using var stream = _transferUtility.OpenStream(
                    _options.Value.DataStoreSettings.S3Endpoint, "microsoftlibs.namespace.lookup.json");
                using var streamReader = new StreamReader(stream);
                var manifest = JsonConvert.DeserializeObject<Dictionary<string, string>>(streamReader.ReadToEnd())
                    .ToDictionary(k => k.Key.ToLower(), v => v.Value);

                var foundPackages = new Dictionary<string, List<PackageVersionPair>>();
                packageVersions
                    .ForEach(p =>
                    {
                        var value = manifest.GetValueOrDefault(p.PackageId.ToLower(), null);
                        if (value != null)
                        {
                            resultsDict.Add(p, new TaskCompletionSource<PackageDetails>());
                            if (!foundPackages.ContainsKey(value))
                            {
                                foundPackages.Add(value, new List<PackageVersionPair>());
                            }
                            foundPackages.GetValueOrDefault(value).Add(p);
                        }
                    });

                _logger.LogInformation("Checking Portability Analyzer source for {0} packages Compatiblity", foundPackages.Count);
                if (foundPackages.Count > 0)
                {
                    Task.Run(() =>
                    {
                        _processConcurrency.Wait();
                        try
                        {
                            ProcessCompatibility(packageVersions, foundPackages, resultsDict);
                        }
                        finally
                        {
                            _processConcurrency.Release();
                        }
                    });
                }

                return resultsDict.ToDictionary(t => t.Key, t => t.Value.Task);
            }
            catch (Exception ex)
            {
                foreach (var packageVersion in packageVersions)
                {
                    if (resultsDict.TryGetValue(packageVersion, out var taskCompletionSource))
                    {
                        taskCompletionSource.TrySetException(
                            new PortingAssistantClientException($"Cannot found package {packageVersion.PackageId} {packageVersion.Version}", ex));
                    }
                }
                return resultsDict.ToDictionary(t => t.Key, t => t.Value.Task);
            }
        }

        /// <summary>
        /// Processes the package compatibility
        /// </summary>
        /// <param name="packageVersions">List of package versions to check</param>
        /// <param name="foundPackages">List of packages found</param>
        /// <param name="resultsDict">The results of the compatibility check to process</param>
        private void ProcessCompatibility(List<PackageVersionPair> packageVersions,
            Dictionary<string, List<PackageVersionPair>> foundPackages,
            Dictionary<PackageVersionPair, TaskCompletionSource<PackageDetails>> resultsDict)
        {
            var foundSet = new HashSet<PackageVersionPair>();
            var errorSet = new HashSet<PackageVersionPair>();

            foreach (var url in foundPackages)
            {
                try
                {
                    _logger.LogInformation("Downloading {0} from {1}", url.Key, _options.Value.DataStoreSettings.S3Endpoint);
                    using var stream = _transferUtility.OpenStream(
                        _options.Value.DataStoreSettings.S3Endpoint, url.Key.Substring(_options.Value.DataStoreSettings.S3Endpoint.Length + 6));
                    using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
                    using var streamReader = new StreamReader(gzipStream);
                    var result = JsonConvert.DeserializeObject<PackageFromS3>(streamReader.ReadToEnd());
                    result.Package.Name = url.Value.First().PackageId;
                    foreach (var packageVersion in url.Value)
                    {
                        if (resultsDict.TryGetValue(packageVersion, out var taskCompletionSource))
                        {
                            taskCompletionSource.SetResult(result.Package);
                            foundSet.Add(packageVersion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed when download and parsing {0} from {1}, {2}", url.Key, _options.Value.DataStoreSettings.S3Endpoint.Length, ex);
                    foreach (var packageVersion in url.Value)
                    {
                        if (resultsDict.TryGetValue(packageVersion, out var taskCompletionSource))
                        {
                            taskCompletionSource.SetException(new PortingAssistantClientException($"Cannot found package {packageVersion.PackageId} {packageVersion.Version}", ex));
                            errorSet.Add(packageVersion);
                        }
                    }
                }
            }

            foreach (var packageVersion in packageVersions)
            {
                if (!foundSet.Contains(packageVersion) && !errorSet.Contains(packageVersion))
                {
                    if (resultsDict.TryGetValue(packageVersion, out var taskCompletionSource))
                    {
                        _logger.LogInformation(
                            $"Can Not Find package {packageVersion.PackageId} " +
                            $"{packageVersion.Version} in external source, check internal source");
                        taskCompletionSource.TrySetException(
                            new PortingAssistantClientException($"Cannot found package {packageVersion.PackageId} {packageVersion.Version}", null));
                    }
                }
            }
        }

        /// <summary>
        /// Returns the type of this compatiblity checker
        /// </summary>
        /// <returns>Type of checker (Portability Analyzer)</returns>
        public PackageSourceType GetCompatibilityCheckerType()
        {
            return PackageSourceType.PORTABILITY_ANALYZER;
        }

        private class PackageFromS3
        {
            public PackageDetails Package { get; set; }
        }
    }

}