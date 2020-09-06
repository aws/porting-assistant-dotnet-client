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
using System.IO.Compression;

namespace PortingAssistant.NuGet
{
    public class RecommendationChecker : IPortingAssistantRecommendationHandler
    {
        private readonly ILogger _logger;
        private readonly IOptions<AnalyzerConfiguration> _options;
        private readonly ITransferUtility _transferUtility;
        private static readonly int _maxProcessConcurrency = 3;
        private static readonly SemaphoreSlim _processConcurrency = new SemaphoreSlim(_maxProcessConcurrency);

        public RecommendationChecker(
            ITransferUtility transferUtility,
            ILogger<ExternalPackagesCompatibilityChecker> logger,
            IOptions<AnalyzerConfiguration> options
            )
        {
            _logger = logger;
            _options = options;
            _transferUtility = transferUtility;
        }

        public Dictionary<string, Task<RecommendationDetails>> GetApiRecommendation(List<string> NameSpaces)
        {
            var resultsDict = new Dictionary<string, TaskCompletionSource<RecommendationDetails>>();
            try
            {
                using var stream = _transferUtility.OpenStream(
                    _options.Value.DataStoreSettings.S3Endpoint, "namespaces.recommendation.lookup.json");
                using var streamReader = new StreamReader(stream);
                var manifest = JsonConvert.DeserializeObject<Dictionary<string, string>>(streamReader.ReadToEnd())
                    .ToDictionary(k => k.Key.ToLower(), v => v.Value);
 
                var foundPackages = new Dictionary<string, List<string>>();
                NameSpaces
                    .ForEach(p =>
                    {
                        var value = manifest.GetValueOrDefault(p.ToLower(), null);
                        if (value != null)
                        {
                            resultsDict.Add(p, new TaskCompletionSource<RecommendationDetails>());
                            if (!foundPackages.ContainsKey(value))
                            {
                                foundPackages.Add(value, new List<string>());
                            }
                            foundPackages.GetValueOrDefault(value).Add(p);
                        }
                    });
 
                _logger.LogInformation("Checking Github files {0} for recommendations", foundPackages.Count);
                if (foundPackages.Count > 0)
                {
                    Task.Run(() =>
                    {
                        _processConcurrency.Wait();
                        try
                        {
                            ProcessCompatibility(NameSpaces, foundPackages, resultsDict);
                        }
                        finally
                        {
                            _processConcurrency.Release();
                        }
                    });
                }
 
                return resultsDict.ToDictionary(t => t.Key, t => t.Value.Task);
            } catch (Exception ex)
            {
                foreach (var NameSpace in NameSpaces)
                {
                    if (resultsDict.TryGetValue(NameSpace, out var taskCompletionSource))
                    {
                        taskCompletionSource.TrySetException(
                            new PortingAssistantClientException($"Cannot found namespace for recommendation {NameSpace}", ex));
                    }
                }
                return resultsDict.ToDictionary(t => t.Key, t => t.Value.Task);
            }
        }
 
        private void ProcessCompatibility(List<string> NameSpaces,
            Dictionary<string, List<string>> foundPackages,
            Dictionary<string, TaskCompletionSource<RecommendationDetails>> resultsDict)
        {
            var foundSet = new HashSet<string>();
            var errorSet = new HashSet<string>();
            try
            {
                foreach (var url in foundPackages)
                {
                    try
                    {
                        // no testing possible using GitHub (Its in provate repo). Using s3 to proceed with testing
                        _logger.LogInformation("Downloading {0} from {1}", url.Key, _options.Value.DataStoreSettings.S3Endpoint);
                        using var stream = _transferUtility.OpenStream(
                            _options.Value.DataStoreSettings.S3Endpoint, url.Key.Substring(_options.Value.DataStoreSettings.S3Endpoint.Length + 6));
                        using var streamReader = new StreamReader(stream);
                        var result = JsonConvert.DeserializeObject<PackageFromGitHub>(streamReader.ReadToEnd());
 
                        foreach (var NameSpace in url.Value)
                        {
                            if (resultsDict.TryGetValue(NameSpace, out var taskCompletionSource))
                            {
                                taskCompletionSource.SetResult(result.RecommendationObject);
                                foundSet.Add(NameSpace);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed when download recommendation and parsing {0} from {1}, {2}", url.Key, _options.Value.DataStoreSettings.S3Endpoint, ex);
                        foreach (var NameSpace in url.Value)
                        {
                            if (resultsDict.TryGetValue(NameSpace, out var taskCompletionSource))
                            {
                                taskCompletionSource.SetException(new PortingAssistantClientException($"Failed process {NameSpace} Namespace", ex));
                                errorSet.Add(NameSpace);
                            }
                        }
                    }
                }
 
                foreach (var NameSpace in NameSpaces)
                {
                    if (!foundSet.Contains(NameSpace) && !errorSet.Contains(NameSpace))
                    {
                        if (resultsDict.TryGetValue(NameSpace, out var taskCompletionSource))
                        {
                            _logger.LogInformation(
                                $"Can Not Find {NameSpace} Recommendation in external source, Discarding this namespace");
                            taskCompletionSource.TrySetException(
                               new PortingAssistantClientException($"Cannot find {NameSpace} Namespace", null));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                foreach (var NameSpace in NameSpaces)
                {
                    if (resultsDict.TryGetValue(NameSpace, out var taskCompletionSource))
                    {
                        taskCompletionSource.TrySetException(
                            new PortingAssistantClientException($"Failed Processing {NameSpace} Namespace", null));
                    }
                }
 
                _logger.LogError("Process Recommendations with Error: {0}", ex);
            }
        }
 
        public PackageSourceType GetCompatibilityCheckerType()
        {
            return PackageSourceType.RECOMMENDATION;
        }
 
        private class PackageFromGitHub
        {
            public RecommendationDetails RecommendationObject { get; set; }
        }
    }
 
}