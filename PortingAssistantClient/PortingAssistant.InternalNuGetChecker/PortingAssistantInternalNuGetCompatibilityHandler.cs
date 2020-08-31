using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using Microsoft.Extensions.Logging;
using PortingAssistant.InternalNuGetChecker.Model;

namespace PortingAssistant.InternalNuGetChecker
{
    public class PortingAssistantInternalNuGetCompatibilityHandler : IPortingAssistantInternalNuGetCompatibilityHandler
    {
        private readonly ILogger _logger;
        private readonly SourceCacheContext cacheContext;

        public PortingAssistantInternalNuGetCompatibilityHandler(ILogger<PortingAssistantInternalNuGetCompatibilityHandler> logger)
        {
            _logger = logger;
            cacheContext = new SourceCacheContext();
        }

        public async Task<CompatibilityResult> CheckCompatibilityAsync(string packageName, string version, string targetFramework, IEnumerable<SourceRepository> internalRepositories)
        {
            if (internalRepositories == null || packageName == null || targetFramework == null)
            {
                throw new ArgumentException("Invalid Parameter");
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
                    package, framework, cacheContext, NuGet.Common.NullLogger.Instance, CancellationToken.None);
                if (packageSource != null)
                {
                    break;
                }
            }

            // Download package
            if (packageSource == null)
            {
                _logger.LogError("Error: No Package Source Found !!!");
                throw new PackageSourceNotFoundException();
            }

            var downloadResource = await packageSource.Source.GetResourceAsync<DownloadResource>();
            var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                packageSource,
                new PackageDownloadContext(cacheContext),
                Path.Combine(tmpPath),
                NuGet.Common.NullLogger.Instance, CancellationToken.None);
            var packageReader = downloadResult.PackageReader;
            var nuspecReader = packageReader.NuspecReader;
            var dependencies = nuspecReader.GetDependencyGroups();

            List<string> dependecyPackages = new List<string>();
            dependecyPackages.AddRange(dependencies.SelectMany(x => x.Packages).Select(p => p.Id));

            // Gather dlls
            var frameworkReducer = new FrameworkReducer();
            var libItems = packageReader.GetLibItems();
            var nearest = libItems.Select(x => frameworkReducer.GetNearest(
                framework, new List<NuGetFramework> { x.TargetFramework })).ToList();
            var isCompatible = libItems.Count() > 0 ?
                nearest.Find(x => x != null) != null :
                frameworkReducer.GetNearest(framework, packageReader.GetSupportedFrameworks()) != null;

            var compatibleDlls = new List<string>();
            compatibleDlls.AddRange(libItems.Where(
                x => nearest.Contains(x.TargetFramework))
                .SelectMany(x => x.Items)
                .Where(x => x.EndsWith("dll", System.StringComparison.OrdinalIgnoreCase))
                .ToList());
            var incompatibleDlls = libItems
                .SelectMany((x) => x.Items)
                .Where((x) => !compatibleDlls.Contains(x))
                .Where(x => x.EndsWith("dll", System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new CompatibilityResult
            {
                IncompatibleDlls = incompatibleDlls,
                CompatibleDlls = compatibleDlls,
                IsCompatible = isCompatible,
                DepedencyPackages = dependecyPackages,
                source = packageSource.Source.PackageSource.Name
            };
        }
    }
}
