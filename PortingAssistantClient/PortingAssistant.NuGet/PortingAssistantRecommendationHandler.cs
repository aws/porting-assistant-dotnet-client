using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PortingAssistant.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Amazon.S3.Transfer;
using System.IO;

namespace PortingAssistant.NuGet
{
    public class PortingAssistantRecommendationHandler : IPortingAssistantRecommendationHandler
    {
        private readonly ILogger _logger;
        private readonly IOptions<AnalyzerConfiguration> _options;
        private readonly ITransferUtility _transferUtility;
        private static readonly int _maxProcessConcurrency = 3;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(_maxProcessConcurrency);
        private const string RecommendationLookupFile = "namespaces.recommendation.lookup.json";

        public PortingAssistantRecommendationHandler(
            ITransferUtility transferUtility,
            ILogger<ExternalPackagesCompatibilityChecker> logger,
            IOptions<AnalyzerConfiguration> options)
        {
            _logger = logger;
            _options = options;
            _transferUtility = transferUtility;
        }

        public Dictionary<string, Task<RecommendationDetails>> GetApiRecommendation(IEnumerable<string> namespaces)
        {
            var recommendationTaskCompletionSources = new Dictionary<string, TaskCompletionSource<RecommendationDetails>>();
            try
            {
                using var stream = _transferUtility.OpenStream(
                    _options.Value.DataStoreSettings.S3Endpoint, RecommendationLookupFile);
                using var streamReader = new StreamReader(stream);
                var manifest = JsonConvert.DeserializeObject<Dictionary<string, string>>(streamReader.ReadToEnd())
                    .ToDictionary(k => k.Key.ToLower(), v => v.Value);

                var foundPackages = new Dictionary<string, List<string>>();
                namespaces.ToList().ForEach(p =>
                {
                    var value = manifest.GetValueOrDefault(p.ToLower(), null);
                    if (value != null)
                    {
                        recommendationTaskCompletionSources.Add(p, new TaskCompletionSource<RecommendationDetails>());
                        if (!foundPackages.ContainsKey(value))
                        {
                            foundPackages.Add(value, new List<string>());
                        }
                        foundPackages[value].Add(p);
                    }
                });

                _logger.LogInformation("Checking Github files {0} for recommendations", foundPackages.Count);
                if (foundPackages.Any())
                {
                    Task.Run(() =>
                    {
                        _semaphore.Wait();
                        try
                        {
                            ProcessCompatibility(namespaces, foundPackages, recommendationTaskCompletionSources);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    });
                }

                return recommendationTaskCompletionSources.ToDictionary(t => t.Key, t => t.Value.Task);
            }
            catch (Exception ex)
            {
                foreach (var @namespace in namespaces)
                {
                    if (recommendationTaskCompletionSources.TryGetValue(@namespace, out var taskCompletionSource))
                    {
                        taskCompletionSource.TrySetException(
                            new PortingAssistantClientException(ExceptionMessage.NamespaceNotFound(@namespace), ex));
                    }
                }
                return recommendationTaskCompletionSources.ToDictionary(t => t.Key, t => t.Value.Task);
            }
        }

        private void ProcessCompatibility(IEnumerable<string> namespaces,
            Dictionary<string, List<string>> foundPackages,
            Dictionary<string, TaskCompletionSource<RecommendationDetails>> recommendationTaskCompletionSources)
        {
            var namespacesFound = new HashSet<string>();
            var namespacesWithErrors = new HashSet<string>();
            try
            {
                foreach (var url in foundPackages)
                {
                    try
                    {
                        _logger.LogInformation("Downloading {0} from {1} recommendation source", url.Key, _options.Value.DataStoreSettings.S3Endpoint);
                        using var stream = _transferUtility.OpenStream(
                            _options.Value.DataStoreSettings.S3Endpoint, "recommendation/" + url.Key);

                        using var streamReader = new StreamReader(stream);
                        var packageFromGithub = JsonConvert.DeserializeObject<RecommendationDetails>(streamReader.ReadToEnd());

                        foreach (var @namespace in url.Value)
                        {
                            if (recommendationTaskCompletionSources.TryGetValue(@namespace, out var taskCompletionSource))
                            {
                                taskCompletionSource.SetResult(packageFromGithub);
                                namespacesFound.Add(@namespace);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed when downloading recommendation and parsing {0} from {1}, {2}", url.Key, _options.Value.DataStoreSettings.S3Endpoint, ex);
                        foreach (var @namespace in url.Value)
                        {
                            if (recommendationTaskCompletionSources.TryGetValue(@namespace, out var taskCompletionSource))
                            {
                                taskCompletionSource.SetException(new PortingAssistantClientException(ExceptionMessage.NamespaceFailedToProcess(@namespace), ex));
                                namespacesWithErrors.Add(@namespace);
                            }
                        }
                    }
                }

                foreach (var @namespace in namespaces)
                {
                    if (namespacesFound.Contains(@namespace) || namespacesWithErrors.Contains(@namespace))
                    {
                        continue;
                    }

                    if (recommendationTaskCompletionSources.TryGetValue(@namespace, out var taskCompletionSource))
                    {
                        var errorMessage = $"Could not find {@namespace} recommendation in external source; discarding this namespace.";
                        _logger.LogInformation(errorMessage);

                        var innerException = new NamespaceNotFoundException(errorMessage);
                        taskCompletionSource.TrySetException(new PortingAssistantClientException(ExceptionMessage.NamespaceNotFound(@namespace), innerException));
                    }
                }
            }
            catch (Exception ex)
            {
                foreach (var @namespace in namespaces)
                {
                    if (recommendationTaskCompletionSources.TryGetValue(@namespace, out var taskCompletionSource))
                    {
                        taskCompletionSource.TrySetException(
                            new PortingAssistantClientException(ExceptionMessage.NamespaceFailedToProcess(@namespace), ex));
                    }
                }

                _logger.LogError("Error encountered while processing recommendations: {0}", ex);
            }
        }

        public PackageSourceType GetCompatibilityCheckerType()
        {
            return PackageSourceType.RECOMMENDATION;
        }

    }
}