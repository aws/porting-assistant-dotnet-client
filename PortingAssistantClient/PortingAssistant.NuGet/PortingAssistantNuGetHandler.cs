using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PortingAssistant.Model;
using Microsoft.Extensions.Logging;

namespace PortingAssistant.NuGet
{
    public class PortingAssistantNuGetHandler : IPortingAssistantNuGetHandler
    {
        private readonly ILogger<IPortingAssistantNuGetHandler> _logger;
        private readonly IEnumerable<ICompatibilityChecker> _checkers;
        private readonly ConcurrentDictionary<PackageVersionPair, PackageVersionPairResult> _resultsDict;

        public PortingAssistantNuGetHandler(
            ILogger<PortingAssistantNuGetHandler> logger,
            IEnumerable<ICompatibilityChecker> checkers
            )
        {
            _logger = logger;
            _checkers = checkers.OrderBy((c) => c.GetCompatibilityCheckerType());
            _resultsDict = new ConcurrentDictionary<PackageVersionPair, PackageVersionPairResult>();
        }

        public Task<PackageDetails> GetPackageDetails(PackageVersionPair package)
        {
            if (_resultsDict.TryGetValue(package, out var packageVersionPairResult))
            {
                return packageVersionPairResult.taskCompletionSource.Task;
            }
            throw new PortingAssistantClientException($"Cannot found package {package.PackageId} {package.Version}", null);
        }

        public Dictionary<PackageVersionPair, Task<PackageDetails>> GetNugetPackages(List<PackageVersionPair> packageVersions, string pathToSolution)
        {

            var packageVersionsToQuery = new List<PackageVersionPair>();

            var tasks = packageVersions.Select(packageVersion =>
            {
                var isNotRunning = _resultsDict.TryAdd(
                    packageVersion,
                    new PackageVersionPairResult
                    {
                        taskCompletionSource = new TaskCompletionSource<PackageDetails>()
                    }
                    );
                if (isNotRunning)
                {
                    packageVersionsToQuery.Add(packageVersion);
                }
                _resultsDict.TryGetValue(packageVersion, out var packageVersionPairResult);

                return new Tuple<PackageVersionPair, Task<PackageDetails>>(packageVersion, packageVersionPairResult.taskCompletionSource.Task);
            }).ToDictionary(t => t.Item1, t => t.Item2);

            _logger.LogInformation("Checking compatibility for {0} packages", packageVersionsToQuery.Count);
            if (packageVersionsToQuery.Count > 0)
            {
                Process(packageVersionsToQuery, pathToSolution);
            }

            return tasks;
        }

        private async void Process(List<PackageVersionPair> packageVersions, string pathToSolution)
        {
            var checkResult = new Dictionary<PackageSourceType, Dictionary<PackageVersionPair, Task<PackageDetails>>>();
            var nextPackageVersions = packageVersions.Aggregate(new Dictionary<string, List<PackageVersionPair>>(), (agg, cur) =>
            {
                if (!agg.ContainsKey(cur.PackageId))
                {
                    agg.Add(cur.PackageId, new List<PackageVersionPair>());
                }
                agg[cur.PackageId].Add(cur);
                return agg;
            });
            var exceptions = new Dictionary<PackageVersionPair, Exception>();
            foreach (var checker in _checkers)
            {
                try
                {
                    var results = checker.CheckAsync(
                        nextPackageVersions.SelectMany(p => p.Value).Distinct().ToList(), pathToSolution);
                    await Task.WhenAll(results.Select(result =>
                    {
                        return result.Value.ContinueWith(task =>
                        {
                            if (task.IsCompletedSuccessfully)
                            {
                                _resultsDict.TryGetValue(result.Key, out var packageVersionPairResult);
                                nextPackageVersions.Remove(result.Key.PackageId);
                                packageVersionPairResult.taskCompletionSource.TrySetResult(task.Result);
                            }
                            else
                            {
                                exceptions.TryAdd(result.Key, task.Exception);
                            }
                        });
                    }).Where(r => r != null).ToList());
                }
                catch (Exception ex)
                {
                    _logger.LogError("Process Package Compatibility Failed with error: {0}", ex);
                }
            }

            foreach (var packageVersion in nextPackageVersions.SelectMany(n => n.Value))
            {
                if (_resultsDict.TryRemove(packageVersion, out var packageVersionPairResult))
                {
                    packageVersionPairResult.taskCompletionSource.TrySetException(
                    exceptions.GetValueOrDefault(
                        packageVersion,
                        new PortingAssistantClientException($"Cannot found package {packageVersion.PackageId} {packageVersion.Version}", null)));
                    packageVersionPairResult.taskCompletionSource.TrySetException(
                    exceptions.GetValueOrDefault(
                        packageVersion,
                        new PortingAssistantClientException($"Cannot found package {packageVersion.PackageId} {packageVersion.Version}", null)));
                }
            }
        }

        private class PackageVersionPairResult
        {
            public TaskCompletionSource<PackageDetails> taskCompletionSource { get; set; }
        }
    }
}
