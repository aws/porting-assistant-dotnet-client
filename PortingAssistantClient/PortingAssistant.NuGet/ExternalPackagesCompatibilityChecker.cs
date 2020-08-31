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
    public class ExternalPackagesCompatibilityChecker : ICompatibilityChecker
    {
        private readonly ILogger _logger;
        private readonly IOptions<EndpointOptions> _options;
        private readonly ITransferUtility _transferUtility;
        private static readonly int _maxProcessConcurrency = 3;
        private static readonly SemaphoreSlim _processConcurrency = new SemaphoreSlim(_maxProcessConcurrency);

        public ExternalPackagesCompatibilityChecker(
            ITransferUtility transferUtility,
            ILogger<ExternalPackagesCompatibilityChecker> logger,
            IOptions<EndpointOptions> options
            )
        {
            _logger = logger;
            _options = options;
            _transferUtility = transferUtility;
        }

        public Dictionary<PackageVersionPair, Task<PackageDetails>> CheckAsync(
            List<PackageVersionPair> packageVersions,
            string pathToSolution
            )
        {
            var resultsDict = packageVersions.Select(packageVersion =>
            {
                return new Tuple<PackageVersionPair, TaskCompletionSource<PackageDetails>>(packageVersion, new TaskCompletionSource<PackageDetails>());
            }).ToDictionary(t => t.Item1, t => t.Item2);

            _logger.LogInformation("Checking external source for {0} packages Compatiblity", packageVersions.Count);
            if (packageVersions.Count > 0)
            {
                Task.Run(() =>
                {
                    _processConcurrency.Wait();
                    try
                    {
                        ProcessCompatibility(packageVersions, resultsDict);
                    }
                    finally
                    {
                        _processConcurrency.Release();
                    }
                });
            }

            return resultsDict.ToDictionary(t => t.Key, t => t.Value.Task);
        }

        private void ProcessCompatibility(List<PackageVersionPair> packageVersions,
            Dictionary<PackageVersionPair, TaskCompletionSource<PackageDetails>> resultsDict)
        {
            var foundSet = new HashSet<PackageVersionPair>();
            var errorSet = new HashSet<PackageVersionPair>();
            try
            {
                var packageVersionsAgg = packageVersions.Aggregate(new Dictionary<string, List<PackageVersionPair>>(), (agg, packageVersion) =>
                {
                    if (!agg.ContainsKey(packageVersion.PackageId))
                    {
                        agg.Add(packageVersion.PackageId, new List<PackageVersionPair>());
                    }
                    agg[packageVersion.PackageId].Add(packageVersion);
                    return agg;
                });
                foreach (var package in packageVersionsAgg)
                {
                    try
                    {
                        _logger.LogInformation("Downloading {0} from {1}", package.Key.ToLower() + ".json.gz", _options.Value.NugetPackageBucket);
                        using var stream = _transferUtility.OpenStream(
                            _options.Value.NugetPackageBucket, package.Key.ToLower() + ".json.gz");
                        using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
                        using var streamReader = new StreamReader(gzipStream);
                        var result = JsonConvert.DeserializeObject<PackageFromS3>(streamReader.ReadToEnd());
                        // Validate result
                        if (result.Package.Name == null || result.Package.Name.Trim().ToLower() != package.Key.Trim().ToLower())
                        {
                            throw new NugetValidationException(result.Package);
                        }
                        foreach (var packageVersion in package.Value)
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
                        _logger.LogError("Failed when download and parsing {0} from {1}, {2}", package.Key.ToLower() + ".json.gz", _options.Value.NugetPackageBucket, ex);
                        foreach (var packageVersion in package.Value)
                        {
                            if (resultsDict.TryGetValue(packageVersion, out var taskCompletionSource))
                            {
                                taskCompletionSource.SetException(new PackageVersionNotFoundException(packageVersion.PackageId, packageVersion.Version, ex));
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
                               new PackageVersionNotFoundException(packageVersion.PackageId, packageVersion.Version, null));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                foreach (var packageVersion in packageVersions)
                {
                    if (resultsDict.TryGetValue(packageVersion, out var taskCompletionSource))
                    {
                        taskCompletionSource.TrySetException(
                            new PackageVersionNotFoundException(packageVersion.PackageId, packageVersion.Version, ex));
                    }
                }

                _logger.LogError("Process Package Compatibility with Error: {0}", ex);
            }
        }

        public CompatibilityCheckerType GetCompatibilityCheckerType()
        {
            return CompatibilityCheckerType.EXTERNAL;
        }

        private class PackageFromS3
        {
            public PackageDetails Package { get; set; }
        }
    }

}