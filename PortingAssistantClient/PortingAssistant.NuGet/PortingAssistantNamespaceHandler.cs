using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Amazon.S3.Transfer;
using System.IO;
using System.IO.Compression;
using PortingAssistant.Model;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PortingAssistant.NuGet
{
    public class NamespaceCompatibilityChecker : IPortingAssistantNamespaceHandler
    {
        private readonly ILogger _logger;
        private readonly IOptions<AnalyzerConfiguration> _configuration;
        private readonly ITransferUtility _transferUtility;
        private readonly ConcurrentDictionary<string, NamespaceResult> _resultsDict;

        public NamespaceCompatibilityChecker(
            ITransferUtility transferUtility,
            ILogger<NamespaceCompatibilityChecker> logger,
            IOptions<AnalyzerConfiguration> configuration
            )
        {
            _logger = logger;
            _transferUtility = transferUtility;
            _configuration = configuration;
            _resultsDict = new ConcurrentDictionary<string, NamespaceResult>();
        }

        public Dictionary<string, Task<PackageDetails>> GetNamespaceDetails(List<string> Namespaces)
        {
            var nameSpacesToQuery = new List<string>();
            var tasks = Namespaces.Select(Namespace =>
            {
                var isNotRunning = _resultsDict.TryAdd(
                    Namespace,
                    new NamespaceResult
                    {
                        TaskCompletionSource = new TaskCompletionSource<PackageDetails>()
                    });
                if (isNotRunning)
                {
                    Process(Namespaces);
                }
                _resultsDict.TryGetValue(Namespace, out var NamespaceResult);

                return new Tuple<string, Task<PackageDetails>>(Namespace, NamespaceResult.TaskCompletionSource.Task);
            }).ToDictionary(t => t.Item1, t=> t.Item2);

            _logger.LogInformation("Checking compatibility for {0} namespaces", nameSpacesToQuery.Count);
            if(nameSpacesToQuery.Count > 0)
            {
                Process(nameSpacesToQuery);
            }
            return tasks;

        }

        private void Process(List<string> Namespaces)
        {
            foreach (var Namespace in Namespaces)
            {
                try
                {
                    _logger.LogInformation("Downloading {0} from {1}", Namespace + ".json.gz", _configuration.Value.DataStoreSettings.S3Endpoint);
                    using var stream = _transferUtility.OpenStream(
                        _configuration.Value.DataStoreSettings.S3Endpoint, Namespace.ToLower() + ".json.gz");
                    using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
                    using var streamReader = new StreamReader(gzipStream);
                    var result = JsonConvert.DeserializeObject<PackageFromS3>(streamReader.ReadToEnd());
                    // Validate result
                    
                    if (result.Namespaces.Name == null || result.Namespaces.Name.Trim().ToLower() != Namespace.Trim().ToLower())
                    {
                        throw new PortingAssistantClientException($"package download did not match {Namespace}", null);
                    }
                    if (_resultsDict.TryGetValue(Namespace, out var namespaceResult))
                    {
                        namespaceResult.TaskCompletionSource.SetResult(result.Namespaces);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed when download and parsing {0} from {1}, {2}", Namespace.ToLower() + ".json.gz", _configuration.Value.DataStoreSettings.S3Endpoint, ex);
                    if (_resultsDict.TryGetValue(Namespace, out var namespaceResult))
                    {
                        namespaceResult.TaskCompletionSource.SetException(ex);
                    }
                }
            }
            
        }

        public PackageSourceType GetCompatibilityCheckerType()
        {
            return PackageSourceType.SDK;
        }

        private class PackageFromS3
        {
            public PackageDetails Namespaces { get; set; }
        }

        private class NamespaceResult
        {
            public TaskCompletionSource<PackageDetails> TaskCompletionSource { get; set; }
        }
    }
}
