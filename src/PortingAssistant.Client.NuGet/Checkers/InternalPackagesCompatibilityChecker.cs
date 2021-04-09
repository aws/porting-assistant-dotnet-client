using System;
using System.IO;
using System.Linq;
using NuGet.Protocol.Core.Types;
using PortingAssistant.Client.NuGet.InternalNuGet;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using NuGet.Common;
using NuGet.Versioning;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using PortingAssistant.Client.Model;
using Settings = NuGet.Configuration.Settings;

namespace PortingAssistant.Client.NuGet
{
    public class InternalPackagesCompatibilityChecker : ICompatibilityChecker
    {
        private readonly IPortingAssistantInternalNuGetCompatibilityHandler _internalNuGetCompatibilityHandler;
        private readonly ILogger<InternalPackagesCompatibilityChecker> _logger;

        public PackageSourceType CompatibilityCheckerType => PackageSourceType.PRIVATE;

        public InternalPackagesCompatibilityChecker(
            IPortingAssistantInternalNuGetCompatibilityHandler internalNuGetCompatibilityHandler,
            ILogger<InternalPackagesCompatibilityChecker> logger)
        {
            _internalNuGetCompatibilityHandler = internalNuGetCompatibilityHandler;
            _logger = logger;
        }

        public Dictionary<PackageVersionPair, Task<PackageDetails>> Check(
            IEnumerable<PackageVersionPair> packageVersions,
            string pathToSolution, bool isIncremental = false, bool incrementalRefresh = false)
        {
            var internalRepositories = GetInternalRepositories(pathToSolution);
            var internalPackages = GetInternalPackagesAsync(packageVersions.ToList(), internalRepositories).Result;

            _logger.LogInformation("Checking internal source for compatibility of {0} package(s)", internalPackages.Count());
            var packageVersionsGroupedByPackageId = internalPackages
                .GroupBy(pv => pv.PackageId)
                .ToDictionary(pvGroup => pvGroup.Key, pvGroup => pvGroup.ToList());

            var processPackageVersionCompatibilityTasks = StartPackageVersionCompatibilityTasks(packageVersionsGroupedByPackageId, internalRepositories);

            var compatibilityResults = new Dictionary<PackageVersionPair, Task<PackageDetails>>();
            processPackageVersionCompatibilityTasks.ForEach(packageVersionCompatibilityTaskPair =>
            {
                var compatibilityTask = packageVersionCompatibilityTaskPair.Key;
                var packageVersionList = packageVersionCompatibilityTaskPair.Value;

                packageVersionList.ForEach(packageVersion =>
                {
                    compatibilityResults.Add(packageVersion, compatibilityTask);
                });
            });

            return compatibilityResults;
        }

        private List<KeyValuePair<Task<PackageDetails>, List<PackageVersionPair>>> StartPackageVersionCompatibilityTasks(
            Dictionary<string, List<PackageVersionPair>> packageVersionsGroupedByPackageId,
            IEnumerable<SourceRepository> internalRepositories)
        {
            var processPackageVersionCompatibilityTasks = packageVersionsGroupedByPackageId
                .Select(groupedPackageVersions =>
                {
                    var packageId = groupedPackageVersions.Key;
                    var packageVersions = groupedPackageVersions.Value;

                    var compatibleVersionSet = new SortedSet<string>();
                    var versionSet = new SortedSet<string>();
                    var taskCompletionSource = new TaskCompletionSource<PackageDetails>();

                    var processCompatibilityTasks = packageVersions.Select(async packageVersionPair =>
                    {
                        var version = packageVersionPair.Version;

                        versionSet.Add(version);
                        var compatibility = await ProcessCompatibility(packageVersionPair, internalRepositories);
                        if (compatibility?.IsCompatible == true)
                        {
                            compatibleVersionSet.Add(version);
                        }
                    }).Where(task => task != null).ToList();

                    GetPackageDetailsAsync(processCompatibilityTasks, packageId, compatibleVersionSet, versionSet, taskCompletionSource);

                    return new KeyValuePair<Task<PackageDetails>, List<PackageVersionPair>>(taskCompletionSource.Task, packageVersions);
                })
                .ToList();

            return processPackageVersionCompatibilityTasks;
        }

        private async void GetPackageDetailsAsync(List<Task> processCompatibilityTasks, string packageId,
            SortedSet<string> compatibleVersions, SortedSet<string> versions,
            TaskCompletionSource<PackageDetails> taskCompletionSource)
        {
            await Task.WhenAll(processCompatibilityTasks.ToArray());
            if (versions.Count == 0)
            {
                taskCompletionSource.SetException(new PortingAssistantClientException(ExceptionMessage.PackageNotFound(packageId), null));
            }
            var packageDetails = new PackageDetails()
            {
                Name = packageId,
                Versions = versions,
                Targets = new Dictionary<string, SortedSet<string>>
                {
                    { "netcoreapp3.1", compatibleVersions }
                },
                Api = new List<ApiDetails>().ToArray()
            };
            taskCompletionSource.SetResult(packageDetails);
        }

        private async Task<InternalNuGetCompatibilityResult> ProcessCompatibility(
            PackageVersionPair packageVersion,
            IEnumerable<SourceRepository> internalRepositories)
        {
            try
            {
                return await _internalNuGetCompatibilityHandler.CheckCompatibilityAsync(
                    packageVersion.PackageId,
                    packageVersion.Version,
                    "netcoreapp3.1",
                    internalRepositories);
            }
            catch (Exception ex) when (ex is PortingAssistantClientException)
            {
                _logger.LogInformation($"Could not check compatibility for package {packageVersion} " +
                                       $"using internal resources. Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error encountered when checking compatibility of package {packageVersion} " +
                                 $"using internal source(s): {ex}");
            }
            return null;
        }

        public virtual IEnumerable<SourceRepository> GetInternalRepositories(string pathToSolution)
        {
            string solutionDirectory = Path.GetDirectoryName(pathToSolution);
            var settings = Settings.LoadDefaultSettings(solutionDirectory);
            var sourceRepositoryProvider = new SourceRepositoryProvider(
                new PackageSourceProvider(settings),
                Repository.Provider.GetCoreV3());

            var repositories = sourceRepositoryProvider
                .GetRepositories()
                .Where(r =>
                    !string.Equals(r.PackageSource.Name, "nuget.org", StringComparison.OrdinalIgnoreCase)
                    && r.PackageSource.IsEnabled
                    && !string.Equals(r.PackageSource.Name, "microsoft visual studio offline packages", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return repositories;
        }

        public async Task<IEnumerable<PackageVersionPair>> GetInternalPackagesAsync(
            IReadOnlyList<PackageVersionPair> packageVersions,
            IEnumerable<SourceRepository> internalRepositories)
        {
            var internalPackages = new List<PackageVersionPair>();
            foreach (var repository in internalRepositories)
            {
                var source = await repository.GetResourceAsync<FindPackageByIdResource>();
                var cacheContext = new SourceCacheContext();

                foreach (var package in packageVersions)
                {
                    try
                    {
                        var packageExists = await source.DoesPackageExistAsync(
                            package.PackageId,
                            new NuGetVersion(package.Version),
                            cacheContext,
                            NullLogger.Instance, CancellationToken.None);
                        if (packageExists)
                        {
                            internalPackages.Add(package);
                        }
                    }
                    catch (Exception error)
                    {
                        _logger.LogError($"Error encountered while accessing internal source repository {repository.PackageSource.Source}: " +
                                         $"Error type: {error.GetType()}\t Error Message: {error.Message}\t Stack trace: {error.StackTrace}");
                    }
                }
            }
            return internalPackages;
        }
    }
}
