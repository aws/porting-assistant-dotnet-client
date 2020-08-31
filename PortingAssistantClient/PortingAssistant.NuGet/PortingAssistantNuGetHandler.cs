using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PortingAssistant.Model;
using Microsoft.Extensions.Logging;
using Semver;

namespace PortingAssistant.NuGet
{
    public enum CompatibilityCheckerType
    {
        EXTERNAL,
        PORTABILITY_ANALYZER,
        PRIVATE
    }

    public class PortingAssistantNuGetHandler : IPortingAssistantNuGetHandler
    {
        private readonly ILogger<IPortingAssistantNuGetHandler> _logger;
        private readonly IEnumerable<ICompatibilityChecker> _checkers;
        private readonly ConcurrentDictionary<PackageVersionPair, PackageVersionPairResult> _resultsDict;
        private const string DEFAULT_TARGET = "netcoreapp3.1";

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
                return packageVersionPairResult.packageTaskCompletionSource.Task;
            }
            throw new PackageVersionNotFoundException(package.PackageId, package.Version, null);
        }

        public Dictionary<PackageVersionPair, Task<PackageVersionResult>> GetNugetPackages(List<PackageVersionPair> packageVersions, string pathToSolution)
        {

            var packageVersionsToQuery = new List<PackageVersionPair>();

            var tasks = packageVersions.Select(packageVersion =>
            {
                var isNotRunning = _resultsDict.TryAdd(
                    packageVersion,
                    new PackageVersionPairResult
                    {
                        taskCompletionSource = new TaskCompletionSource<PackageVersionResult>(),
                        packageTaskCompletionSource = new TaskCompletionSource<PackageDetails>()
                    }
                    );
                if (isNotRunning)
                {
                    packageVersionsToQuery.Add(packageVersion);
                }
                _resultsDict.TryGetValue(packageVersion, out var packageVersionPairResult);

                return new Tuple<PackageVersionPair, Task<PackageVersionResult>>(packageVersion, packageVersionPairResult.taskCompletionSource.Task);
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
            var checkResult = new Dictionary<CompatibilityCheckerType, Dictionary<PackageVersionPair, Task<PackageDetails>>>();
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
                                var compatResult = isCompatible(task.Result, result.Key);
                                packageVersionPairResult.packageTaskCompletionSource.TrySetResult(task.Result);
                                packageVersionPairResult.taskCompletionSource.TrySetResult(new PackageVersionResult
                                {
                                    PackageId = result.Key.PackageId,
                                    Version = result.Key.Version,
                                    Compatible = compatResult.compatible,
                                    packageUpgradeStrategies = compatResult.upgradeOptions
                                });
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
                        new PackageVersionNotFoundException(packageVersion.PackageId, packageVersion.Version, null)));
                    packageVersionPairResult.packageTaskCompletionSource.TrySetException(
                    exceptions.GetValueOrDefault(
                        packageVersion,
                        new PackageVersionNotFoundException(packageVersion.PackageId, packageVersion.Version, null)));
                }
            }
        }

        private compatibleResult isCompatible(PackageDetails packageDetails, PackageVersionPair packageVersionPair, string target = DEFAULT_TARGET)
        {
            try
            {
                var foundTarget = packageDetails.Targets.GetValueOrDefault(target, null);
                if (foundTarget == null)
                {
                    return new compatibleResult
                    {
                        compatible = Compatibility.INCOMPATIBLE,
                        upgradeOptions = new List<string>()
                    };
                }
                if (!SemVersion.TryParse(packageVersionPair.Version, out var version))
                {
                    return new compatibleResult
                    {
                        compatible = Compatibility.UNKNOWN,
                        upgradeOptions = new List<string>()
                    };
                }
                return new compatibleResult
                {
                    compatible = foundTarget.Any(v => SemVersion.Compare(version, SemVersion.Parse(v)) >= 0) ? Compatibility.COMPATIBLE : Compatibility.INCOMPATIBLE,
                    upgradeOptions = foundTarget.Where(v => SemVersion.Compare(SemVersion.Parse(v), version) > 0).ToList()
                };
            }
            catch (Exception e)
            {
                _logger.LogError("parse package version {0} {1}with error {2}", packageVersionPair.PackageId, packageVersionPair.Version, e);
                return new compatibleResult
                {
                    compatible = Compatibility.UNKNOWN,
                    upgradeOptions = new List<string>()
                };
            }

        }

        private class PackageVersionPairResult
        {
            public TaskCompletionSource<PackageVersionResult> taskCompletionSource { get; set; }
            public TaskCompletionSource<PackageDetails> packageTaskCompletionSource { get; set; }
        }

        private class compatibleResult
        {
            public Compatibility compatible { get; set; }
            public List<string> upgradeOptions { get; set; }
        }
    }
}
