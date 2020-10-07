using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.NuGet
{
    public class PortingAssistantNuGetHandler : IPortingAssistantNuGetHandler
    {
        private readonly ILogger<IPortingAssistantNuGetHandler> _logger;
        private readonly IEnumerable<ICompatibilityChecker> _compatibilityCheckers;
        private readonly ConcurrentDictionary<PackageVersionPair, TaskCompletionSource<PackageDetails>> _compatibilityTaskCompletionSources;

        public PortingAssistantNuGetHandler(
            ILogger<PortingAssistantNuGetHandler> logger,
            IEnumerable<ICompatibilityChecker> compatibilityCheckers)
        {
            _logger = logger;
            _compatibilityCheckers = compatibilityCheckers.OrderBy((c) => c.CompatibilityCheckerType);
            _compatibilityTaskCompletionSources = new ConcurrentDictionary<PackageVersionPair, TaskCompletionSource<PackageDetails>>();
        }

        public Dictionary<PackageVersionPair, Task<PackageDetails>> GetNugetPackages(List<PackageVersionPair> packageVersions, string pathToSolution)
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

            _logger.LogInformation("Checking compatibility for {0} packages", packageVersionsToQuery.Count);
            Process(packageVersionsToQuery, pathToSolution);

            return tasks;
        }

        private async void Process(List<PackageVersionPair> packageVersions, string pathToSolution)
        {
            if (!packageVersions.Any())
            {
                _logger.LogInformation("No package version compatibilities to process.");
                return;
            }

            var packageVersionsGroupedByPackageId = packageVersions
                .GroupBy(pv => pv.PackageId)
                .ToDictionary(pvGroup => pvGroup.Key, pvGroup => pvGroup.ToList());

            var distinctPackageVersions = packageVersions.Distinct().ToList();
            var exceptions = new Dictionary<PackageVersionPair, Exception>();
            foreach (var compatibilityChecker in _compatibilityCheckers)
            {
                try
                {
                    var compatibilityResults = compatibilityChecker.CheckAsync(distinctPackageVersions, pathToSolution);
                    await Task.WhenAll(compatibilityResults.Select(result =>
                    {
                        return result.Value.ContinueWith(task =>
                        {
                            if (task.IsCompletedSuccessfully)
                            {
                                packageVersionsGroupedByPackageId.Remove(result.Key.PackageId);
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
                    _logger.LogError("Package compatibility processing failed with error: {0}", ex);
                }
            }

            foreach (var packageVersion in packageVersionsGroupedByPackageId.SelectMany(packageVersionGroup => packageVersionGroup.Value))
            {
                if (_compatibilityTaskCompletionSources.TryRemove(packageVersion, out var packageVersionPairResult))
                {
                    var defaultErrorMessage = $"Could not find package {packageVersion}. Compatibility task status: {packageVersionPairResult.Task.Status}.";
                    var defaultException = new PortingAssistantClientException(ExceptionMessage.PackageNotFound(packageVersion), new PackageNotFoundException(defaultErrorMessage));
                    var exception = exceptions.GetValueOrDefault(packageVersion, defaultException);

                    packageVersionPairResult.TrySetException(exception);
                }
                else
                {
                    _logger.LogInformation($"Attempted to remove package {packageVersion} from compatibility tasks but it was not found.");
                }
            }
        }
    }
}
