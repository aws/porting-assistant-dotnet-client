using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Model;
using NuGet.Common;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace PortingAssistant.Client.NuGet.InternalNuGet
{
    public class PortingAssistantInternalNuGetCompatibilityHandler : IPortingAssistantInternalNuGetCompatibilityHandler
    {
        private readonly ILogger _logger;
        private readonly SourceCacheContext _cacheContext;

        public PortingAssistantInternalNuGetCompatibilityHandler(ILogger<PortingAssistantInternalNuGetCompatibilityHandler> logger)
        {
            _logger = logger;
            _cacheContext = new SourceCacheContext();
        }

        public async Task<InternalNuGetCompatibilityResult> CheckCompatibilityAsync(string packageName, string version, string targetFramework, IEnumerable<SourceRepository> internalRepositories)
        {
            if (packageName == null || targetFramework == null || internalRepositories == null)
            {
                var invalidParamNames = new List<string>();
                if (packageName == null)
                {
                    invalidParamNames.Add(nameof(packageName));
                }
                if (targetFramework == null)
                {
                    invalidParamNames.Add(nameof(targetFramework));
                }
                if (internalRepositories == null)
                {
                    invalidParamNames.Add(nameof(internalRepositories));
                }

                throw new ArgumentException($"Invalid parameter(s) found. The following parameters " +
                                            $"cannot be null: {string.Join(", ", invalidParamNames)}");
            }

            string tmpPath = Path.GetTempPath();
            var framework = NuGetFramework.Parse(targetFramework);
            var package = new PackageIdentity(packageName, NuGetVersion.Parse(version));

            // Get package information from Nuget
            SourcePackageDependencyInfo packageSource = null;
            foreach (var sourceRepository in internalRepositories)
            {
                var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
                packageSource = await dependencyInfoResource.ResolvePackage(
                    package, framework, _cacheContext, NullLogger.Instance, CancellationToken.None);

                if (packageSource != null)
                {
                    break;
                }
            }
            if (packageSource == null)
            {
                var errorMessage = $"Error: No package source found for {package}.";
                _logger.LogError(errorMessage);

                var innerException = new PackageSourceNotFoundException(errorMessage);
                throw new PortingAssistantClientException(ExceptionMessage.PackageSourceNotFound(package), innerException);
            }

            // Download package
            var downloadResource = await packageSource.Source.GetResourceAsync<DownloadResource>();
            var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                packageSource,
                new PackageDownloadContext(_cacheContext),
                Path.Combine(tmpPath),
                NullLogger.Instance, CancellationToken.None);
            var packageReader = downloadResult.PackageReader;
            var nuspecReader = packageReader.NuspecReader;

            var dependencies = nuspecReader.GetDependencyGroups();
            var dependencyPackages = dependencies
                .SelectMany(d => d.Packages)
                .Select(p => p.Id)
                .ToList();

            // Gather dlls
            var frameworkReducer = new FrameworkReducer();
            var libItems = packageReader.GetLibItems();
            var nearestTargetFrameworks = libItems
                .Select(li =>
                    frameworkReducer.GetNearest(
                        framework,
                        new List<NuGetFramework> { li.TargetFramework }))
                .ToList();

            var isCompatible = libItems.Any() ? nearestTargetFrameworks.Any(nugetFramework => nugetFramework != null)
                : frameworkReducer.GetNearest(framework, packageReader.GetSupportedFrameworks()) != null;

            var compatibleDlls = libItems
                .Where(li => nearestTargetFrameworks.Contains(li.TargetFramework))
                .SelectMany(li => li.Items)
                .Where(s => s.EndsWith("dll", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var incompatibleDlls = libItems
                .SelectMany(li => li.Items)
                .Where(s => !compatibleDlls.Contains(s)
                            && s.EndsWith("dll", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new InternalNuGetCompatibilityResult
            {
                IncompatibleDlls = incompatibleDlls,
                CompatibleDlls = compatibleDlls,
                IsCompatible = isCompatible,
                DependencyPackages = dependencyPackages,
                Source = packageSource.Source.PackageSource.Name
            };
        }
    }
}
