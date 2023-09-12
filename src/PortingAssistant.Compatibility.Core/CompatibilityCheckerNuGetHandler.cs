using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using PortingAssistant.Compatibility.Common.Model.Exception;

namespace PortingAssistant.Compatibility.Core
{
    public class CompatibilityCheckerNuGetHandler : ICompatibilityCheckerNuGetHandler
    {
        private readonly IEnumerable<ICompatibilityChecker> _compatibilityCheckers;
        private readonly ConcurrentDictionary<PackageVersionPair, TaskCompletionSource<PackageDetails>> _compatibilityTaskCompletionSources;
        private readonly ILogger _logger;
        public CompatibilityCheckerNuGetHandler(
            IEnumerable<ICompatibilityChecker> compatibilityCheckers,
            ILogger<CompatibilityCheckerNuGetHandler> logger
            )
        {
            _compatibilityCheckers = compatibilityCheckers.OrderBy((c) => c.CompatibilityCheckerType);
            _compatibilityTaskCompletionSources = new ConcurrentDictionary<PackageVersionPair, TaskCompletionSource<PackageDetails>>();
            _logger = logger;
        }

        public Dictionary<PackageVersionPair, Task<PackageDetails>> GetNugetPackages(List<PackageVersionPair> packageVersions)
        {
            var packageVersionsToQuery = new List<PackageVersionPair>();
            var tasks = packageVersions.Select(packageVersion =>
            {
                var isNewCompatibilityTask = _compatibilityTaskCompletionSources.TryAdd(packageVersion, new TaskCompletionSource<PackageDetails>());
                if (isNewCompatibilityTask)
                {
                    packageVersionsToQuery.Add(packageVersion);
                }

                var packageVersionPairResult = _compatibilityTaskCompletionSources[packageVersion];

                return new Tuple<PackageVersionPair, Task<PackageDetails>>(packageVersion, packageVersionPairResult.Task);
            }).ToDictionary(t => t.Item1, t => t.Item2);

            _logger.LogInformation($"Checking compatibility for {packageVersionsToQuery.Count} packages");
            Process( packageVersionsToQuery);

            return tasks;
        }

        private async void Process( List<PackageVersionPair> packageVersions)
        {
            if (!packageVersions.Any())
            {
                _logger.LogInformation("No package version compatibilities to process.");
                return;
            }

            var packageVersionsGroupedByPackageIdDict = packageVersions.ToDictionary(t => t.ToString(), t => t);
            ConcurrentDictionary<string, PackageVersionPair> packageVersionsGroupedByPackageIdConcurrent = new ConcurrentDictionary<string, PackageVersionPair>(packageVersionsGroupedByPackageIdDict);

            var distinctPackageVersions = packageVersions.Distinct().ToList();
            var exceptions = new ConcurrentDictionary<PackageVersionPair, Exception>();

            // The Check function goes through the 3 checkers.
            // Checking order is: SdkCompatibilityChecker -> NugetCompatibilityChecker -> PortabilityAnalyzerCompatibilityChecker
            foreach (var compatibilityChecker in _compatibilityCheckers) 
            {
                try
                {
                    var compatibilityResults = await compatibilityChecker.Check( distinctPackageVersions);
                    await Task.WhenAll(compatibilityResults.Select(result =>
                    {
                        return result.Value.ContinueWith(task =>
                        {
                            if (task.IsCompletedSuccessfully)
                            {
                                packageVersionsGroupedByPackageIdConcurrent.TryRemove(result.Key.ToString(), out _);
                                if (_compatibilityTaskCompletionSources.TryGetValue(result.Key, out var packageVersionPairResult))
                                {
                                    packageVersionPairResult.TrySetResult(task.Result);
                                }
                                else
                                {
                                    throw new ArgumentNullException($"Package version {result.Key} not found in compatibility tasks.");
                                }
                            }
                            else
                            {
                                exceptions.TryAdd(result.Key, task.Exception);
                            }
                        });
                    }).ToList());
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Package compatibility processing failed with error: {ex}");
                }
            }

            // Exceptions. 
            foreach (var packageVersion in packageVersionsGroupedByPackageIdConcurrent.Select(packageVersionGroup => packageVersionGroup.Value))
            {
                if (packageVersion != null && _compatibilityTaskCompletionSources.TryGetValue(packageVersion, out var packageVersionPairResult))
                {
                    var defaultErrorMessage = $"Could not find package {packageVersion} in all sources. Compatibility task status: {packageVersionPairResult.Task.Status}.";

                    if (exceptions.TryGetValue(packageVersion, out var exception))
                    {
                        var newException = new PortingAssistantClientException(defaultErrorMessage,
                            (exception.InnerException is PortingAssistantClientException) ? null : exception.InnerException);
                        packageVersionPairResult.TrySetException(newException);
                    }
                    else
                    {
                        var defaultException = new PortingAssistantClientException(ExceptionMessage.PackageNotFound(packageVersion), new PackageNotFoundException(defaultErrorMessage));
                        packageVersionPairResult.TrySetException(defaultException);
                    }
                }
                else
                {
                    _logger.LogInformation($"Attempted to get package {packageVersion} from compatibility tasks but it was not found.");
                }
            }
        }
    }
}
