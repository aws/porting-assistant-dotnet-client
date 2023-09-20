using System.IO.Compression;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using PortingAssistant.Compatibility.Common.Model.Exception;

namespace PortingAssistant.Compatibility.Core.Checkers
{
    /// <summary>
    /// Compatibility checker for Portability Analyzer results
    /// </summary>
    public class PortabilityAnalyzerCompatibilityChecker : ICompatibilityChecker
    {
        private const string NamespaceLookupFile = "microsoftlibs.namespace.lookup.json";
        private readonly IHttpService _httpService;
        private Dictionary<string, string> _manifest;
        private static readonly int _maxProcessConcurrency = 3;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(_maxProcessConcurrency);
        private ILogger _logger;
        public PackageSourceType CompatibilityCheckerType => PackageSourceType.PORTABILITY_ANALYZER;

        /// <summary>
        /// Creates a new instance of Portability Analyzer compatibility checker
        /// </summary>
        /// <param name="httpService">The transferUtility object to read data from S3</param>
        public PortabilityAnalyzerCompatibilityChecker(
            IHttpService httpService,
            ILogger<PortabilityAnalyzerCompatibilityChecker> logger
            )
        {
            _httpService = httpService;
            _manifest = null;
            _logger = logger;
        }

        /// <summary>
        /// Checks the packages in Portability Analyzer
        /// </summary>
        /// <param name="packageVersions">The package versions to check</param>
        /// <returns>The results of the compatibility check</returns>
        public async Task<Dictionary<PackageVersionPair, Task<PackageDetails>>> Check(
            IEnumerable<PackageVersionPair> packageVersions)
        
        {
            var compatibilityTaskCompletionSources = new Dictionary<PackageVersionPair, TaskCompletionSource<PackageDetails>>();

            try
            {
                if (_manifest == null)
                {
                    _manifest = GetManifestAsync().Result;
                }
                var foundPackages = new Dictionary<string, List<PackageVersionPair>>();
                packageVersions.ToList().ForEach(p =>
                {
                    if (p.PackageSourceType != PackageSourceType.SDK)
                    {
                        return;
                    }

                    var value = _manifest.GetValueOrDefault(p.PackageId, null);
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

                _logger.LogInformation($"Checking Portability Analyzer source for compatibility of {foundPackages.Count} package(s)");
                if (foundPackages.Any())
                {
                    _ = Task.Run(() =>
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
        private async void ProcessCompatibility( IEnumerable<PackageVersionPair> packageVersions,
            Dictionary<string, List<PackageVersionPair>> foundPackages,
            Dictionary<PackageVersionPair, TaskCompletionSource<PackageDetails>> compatibilityTaskCompletionSources)
        {
            var packageVersionsFound = new HashSet<PackageVersionPair>();
            var packageVersionsWithErrors = new HashSet<PackageVersionPair>();

            foreach (var url in foundPackages)
            {
                try
                {
                    _logger.LogInformation($"Downloading {url.Key} from {CompatibilityCheckerType}");
                    using var stream = await _httpService.DownloadS3FileAsync(url.Key);
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
                    if (ex.Message.Contains("404"))
                    {
                        _logger.LogInformation($"Encountered {ex.GetType()} while downloading and parsing {url.Key} " +
                                              $"from {CompatibilityCheckerType}, but it was ignored. " +
                                              $"ErrorMessage: {ex.Message}.");
                        // filter all 404 errors
                        ex = null;
                    }
                    else
                    {
                        _logger.LogError($"Failed when downloading and parsing {url.Key} from {CompatibilityCheckerType}, {ex}");
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
            // Download the lookup file "microsoftlibs.namespace.lookup.json" from S3.
            using var stream = await _httpService.DownloadS3FileAsync(NamespaceLookupFile);
            using var streamReader = new StreamReader(stream);
            var result = streamReader.ReadToEnd();
            return JsonConvert.DeserializeObject<JObject>(result).ToObject<Dictionary<string, string>>();
        }

        private class PackageFromS3
        {
            public PackageDetails Package { get; set; }
        }
    }
}
