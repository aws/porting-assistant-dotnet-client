using Newtonsoft.Json;
using System.IO.Compression;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using PortingAssistant.Compatibility.Common.Model.Exception;
using Microsoft.Extensions.Logging;

namespace PortingAssistant.Compatibility.Core.Checkers
{
    public class ExternalCompatibilityChecker : ICompatibilityChecker
    {
        private readonly IRegionalDatastoreService _regionalDatastoreService;
        private static readonly int _maxProcessConcurrency = 3;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(_maxProcessConcurrency);
        private ILogger _logger;

        public virtual PackageSourceType CompatibilityCheckerType => PackageSourceType.NUGET;

        public ExternalCompatibilityChecker(
            IRegionalDatastoreService regionalDatastoreService,
            ILogger<ExternalCompatibilityChecker> logger)
        {
            _regionalDatastoreService = regionalDatastoreService;
            _logger = logger;
        }

        public async Task<Dictionary<PackageVersionPair, Task<PackageDetails>>> Check(
             IEnumerable<PackageVersionPair> packageVersions)
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

            _logger.LogInformation($"Checking {CompatibilityCheckerType} for compatibility of {packagesToCheck.Count()} package(s)");
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

        private async void ProcessCompatibility( IEnumerable<PackageVersionPair> packageVersions,
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
                var fileToDownload = GetDownloadFilePathV2(CompatibilityCheckerType, packageToDownload);

                try
                {
                    HashSet<string>? apis = null; // OriginalDefinition
                    PackageDetails packageDetails = null;
                    packageDetails = await GetPackageDetailFromS3(fileToDownload, apis); 

                    if (packageDetails == null || packageDetails.Name == null || !string.Equals(packageDetails.Name.Trim().ToLower(),
                        packageToDownload.Trim().ToLower(), StringComparison.OrdinalIgnoreCase))
                    {
                        throw new PackageDownloadMismatchException(
                            actualPackage: packageDetails?.Name,
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
                catch (OutOfMemoryException ex)
                {
                    _logger.LogError($"Failed when downloading and parsing {fileToDownload} from {CompatibilityCheckerType}, {ex}");
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404"))
                    {
                        _logger.LogInformation($"Encountered {ex.GetType()} while downloading and parsing {fileToDownload} " +
                                               $"from {CompatibilityCheckerType}, but it was ignored. Details: {ex.Message}.");
                        // filter all 404 errors
                        ex = null;
                    }
                    else
                    {
                        _logger.LogError($"Failed when downloading and parsing {fileToDownload} from {CompatibilityCheckerType}, {ex}");
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

        private string GetDownloadFilePathV2(PackageSourceType CompatibilityCheckerType, string packageToDownload)
        {
            var fileToDownload = $"{packageToDownload}.json.gz";
            var downloadFilePath = fileToDownload;
            switch (CompatibilityCheckerType)
            {
                case PackageSourceType.NUGET:
                    downloadFilePath = packageToDownload +"/" + fileToDownload;
                    break;
                case PackageSourceType.SDK:
                    downloadFilePath = "namespaces" + "/" + fileToDownload;
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


        public async Task<PackageDetails> GetPackageDetailFromS3(string fileToDownload, HashSet<string> apis = null)
        {
            using var stream = await _regionalDatastoreService.DownloadRegionalS3FileAsync(fileToDownload, isRegionalCall: true);
            if (stream == null)
            {
                return null;
            }
            using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            using var streamReader = new StreamReader(gzipStream);
            var data = JsonConvert.DeserializeObject<PackageFromS3>(streamReader.ReadToEnd());
            var packageDetails = data.Package ?? data.Namespaces;
            // api filter. Only details of the apis from the input file will be returned instead of returning all apis details of the package. 
            if (apis != null && apis.Count > 0)
            {
                var selectedApiDetails = packageDetails.Api.Where(c => apis.Contains(c.MethodSignature));
                packageDetails.Api = selectedApiDetails.ToArray();
            }
            return packageDetails;
        }

    }
}