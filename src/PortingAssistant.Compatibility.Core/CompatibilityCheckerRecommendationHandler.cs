using Newtonsoft.Json;
using Amazon.Lambda.Core;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PortingAssistant.Compatibility.Common.Model.Exception;

namespace PortingAssistant.Compatibility.Core
{
    // The CompatibilityCheckerRecommendationHandler checks and gets recommendation file details ("namespace.recommendation.json") from the datastore, if any.
    public class CompatibilityCheckerRecommendationHandler : ICompatibilityCheckerRecommendationHandler
    {
        private readonly ILogger _logger;
        private readonly IHttpService _httpService;
        private static readonly int _maxProcessConcurrency = 3;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(_maxProcessConcurrency);
        private const string RecommendationLookupFile = "namespaces.recommendation.lookup.json";
        private Dictionary<string, string> _manifest;

        public PackageSourceType CompatibilityCheckerType => PackageSourceType.RECOMMENDATION;


        public CompatibilityCheckerRecommendationHandler(
            IHttpService httpService,
            ILogger<CompatibilityCheckerRecommendationHandler> logger
            )
        {
            _logger = logger;
            _httpService = httpService;
            _manifest = null;
        }

        public Dictionary<string, Task<RecommendationDetails>> GetApiRecommendation(IEnumerable<string> namespaces)
        {
            var recommendationTaskCompletionSources = new Dictionary<string, TaskCompletionSource<RecommendationDetails>>();
            try
            {
                if (_manifest == null)
                {
                    var manifestTask = GetManifestAsync();
                    manifestTask.Wait();
                    _manifest = manifestTask.Result;
                }
                var foundPackages = new Dictionary<string, List<string>>();
                namespaces.ToList().ForEach(p =>
                {
                    var value = _manifest.GetValueOrDefault(p, null);
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

        private async void ProcessCompatibility(IEnumerable<string> namespaces,
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
                        _logger.LogInformation("Downloading {0} from {1}", url.Key, CompatibilityCheckerType);
                        using var stream = await _httpService.DownloadGitHubFileAsync("recommendation/" + url.Key);

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
                        _logger.LogError("Failed when downloading recommendation and parsing {0} from {1}, {2}", url.Key, CompatibilityCheckerType, ex);
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

        private async Task<Dictionary<string, string>> GetManifestAsync()
        {
            using var stream = await _httpService.DownloadGitHubFileAsync("data/" + RecommendationLookupFile);
            using var streamReader = new StreamReader(stream);
            return JsonConvert.DeserializeObject<JObject>(streamReader.ReadToEnd()).ToObject<Dictionary<string, string>>();
        }
    }

}