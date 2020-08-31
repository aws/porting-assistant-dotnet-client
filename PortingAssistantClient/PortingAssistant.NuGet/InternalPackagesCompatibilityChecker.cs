using System;
using System.IO;
using System.Linq;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using PortingAssistant.InternalNuGetChecker;
using PortingAssistant.InternalNuGetChecker.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using NuGet.Common;
using NuGet.Versioning;
using Microsoft.Extensions.Logging;
using PortingAssistant.Model;

namespace PortingAssistant.NuGet
{
    public class InternalPackagesCompatibilityChecker : ICompatibilityChecker
    {
        private readonly IPortingAssistantInternalNuGetCompatibilityHandler _checker;
        private readonly ILogger<InternalPackagesCompatibilityChecker> _logger;

        public InternalPackagesCompatibilityChecker(IPortingAssistantInternalNuGetCompatibilityHandler checker,
            ILogger<InternalPackagesCompatibilityChecker> logger
            )
        {
            _checker = checker;
            _logger = logger;
        }

        public Dictionary<PackageVersionPair, Task<PackageDetails>> CheckAsync(
                   List<PackageVersionPair> packageVersions,
                   string pathToSolution
                   )
        {
            var internalRepositories = GetInternalRepository(pathToSolution);
            var internalPackages = getInternalPackagesAsync(pathToSolution, packageVersions, internalRepositories).Result;

            _logger.LogInformation("check internal source for {0} packages Compatiblity ", internalPackages.Count);
            var tasks = internalPackages.Aggregate(new Dictionary<string, List<PackageVersionPair>>(), (agg, packageVersion) =>
            {
                if (!agg.ContainsKey(packageVersion.PackageId))
                {
                    agg.Add(packageVersion.PackageId, new List<PackageVersionPair>());
                }
                agg[packageVersion.PackageId].Add(packageVersion);
                return agg;
            })
            .Select(packageVersionAgg =>
            {
                var compatibleNameValueSet = new SortedSet<string>();
                var versionSet = new SortedSet<string>();
                var taskcompletionSource = new TaskCompletionSource<PackageDetails>();

                var tasks = packageVersionAgg.Value.Select(async packageVersion =>
                {
                    versionSet.Add(packageVersion.Version);
                    var compatibility = await processCompatibility(packageVersion, internalRepositories);
                    if (compatibility != null && compatibility.IsCompatible)
                    {
                        compatibleNameValueSet.Add(packageVersion.Version);
                    }
                }).Where(task => task != null).ToList();

                getPackageDetailsAsync(tasks, packageVersionAgg.Key, compatibleNameValueSet, versionSet, taskcompletionSource);

                return new KeyValuePair<Task<PackageDetails>, List<PackageVersionPair>>(taskcompletionSource.Task, packageVersionAgg.Value);
            })
            .ToList();

            var results = new Dictionary<PackageVersionPair, Task<PackageDetails>>();
            tasks.ForEach(taskPair =>
            {
                taskPair.Value.ForEach(packageVersion =>
                {
                    results.Add(packageVersion, taskPair.Key);
                });
            });

            return results;
        }

        private async void getPackageDetailsAsync(List<Task> tasks, string packageId,
            SortedSet<string> compatible, SortedSet<string> versions,
            TaskCompletionSource<PackageDetails> taskCompletionSource)
        {
            await Task.WhenAll(tasks.ToArray());
            if (versions.Count == 0)
            {
                taskCompletionSource.SetException(new PackageVersionNotFoundException(packageId, null, null));
            }
            var packageDetails = new PackageDetails()
            {
                Name = packageId,
                Versions = versions,
                Targets = new Dictionary<string, SortedSet<string>>
                {
                    { "netcoreapp3.1", compatible }
                },
                Api = new List<ApiDetails>().ToArray()
            };
            taskCompletionSource.SetResult(packageDetails);
        }

        private async Task<CompatibilityResult> processCompatibility(
            PackageVersionPair packageVersion,
            IEnumerable<SourceRepository> internalRepositories)
        {
            try
            {
                return await _checker.CheckCompatibilityAsync(
                    packageVersion.PackageId,
                    packageVersion.Version,
                    "netcoreapp3.1",
                    internalRepositories
                    );
            }
            catch (Exception error) when (error is OperationCanceledException || error is PackageSourceNotFoundException)
            {
                _logger.LogInformation($"Can Not Check for package {packageVersion.PackageId} {packageVersion.Version} with error: {error.Message}");
            }
            catch (Exception error)
            {
                _logger.LogError($"Internal Package Compatibility for " +
                    $"{packageVersion.PackageId} {packageVersion.Version} with error : {error}");
            }
            return null;
        }

        public virtual IEnumerable<SourceRepository> GetInternalRepository(string pathToSolution)
        {
            string solutionFolderPath = Path.GetDirectoryName(pathToSolution);
            var setting = Settings.LoadDefaultSettings(solutionFolderPath);
            var sourceRepositoryProvider = new SourceRepositoryProvider(
                new PackageSourceProvider(setting),
                Repository.Provider.GetCoreV3());

            var repositories = sourceRepositoryProvider
                .GetRepositories()
                .Where(r => r.PackageSource.Name.ToLower() != "nuget.org" && r.PackageSource.IsEnabled && r.PackageSource.Name.ToLower() != "microsoft visual studio offline packages");

            return repositories;
        }

        public async Task<List<PackageVersionPair>> getInternalPackagesAsync(
            string pathToSolution,
            IReadOnlyList<PackageVersionPair> packageVersions,
            IEnumerable<SourceRepository> internalRepositories)
        {
            var internalPackages = new List<PackageVersionPair>();

            foreach (var repository in internalRepositories)
            {
                var source = await repository.GetResourceAsync<FindPackageByIdResource>();
                SearchFilter searchFilter = new SearchFilter(includePrerelease: true);
                var cacheContext = new SourceCacheContext();

                foreach (var package in packageVersions)
                {
                    try
                    {
                        var result = await source.DoesPackageExistAsync(
                            package.PackageId,
                            new NuGetVersion(package.Version),
                            cacheContext,
                            NullLogger.Instance, CancellationToken.None);
                        if (result)
                        {
                            internalPackages.Add(package);
                        }
                    }
                    catch (OperationCanceledException error)
                    {
                        _logger.LogInformation($"Check for package {package.Version} {package.PackageId} was cancelled: {error.Message}");
                        continue;
                    }
                    catch (Exception error)
                    {
                        _logger.LogError($"Access internal source repository {repository.PackageSource.Source} with error: {error.Message} {error.GetType()} {error.StackTrace}");
                    }
                }
            }
            return internalPackages;
        }

        public CompatibilityCheckerType GetCompatibilityCheckerType()
        {
            return CompatibilityCheckerType.PRIVATE;
        }
    }
}
