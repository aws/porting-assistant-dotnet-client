using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Amazon.S3.Transfer;
using System.IO;
using System.IO.Compression;
using PortingAssistant.Model;
using System.Collections.Concurrent;

namespace PortingAssistant.NuGet
{
    public class NamespaceCompatibilityChecker
    {
        private readonly ILogger _logger;
        private readonly IOptions<AnalyzerConfiguration> _configuration;
        private readonly ITransferUtility _transferUtility;
        private readonly ConcurrentDictionary<string, NamespaceResult> _resultsDict;
        private static readonly int _maxProcessConcurrency = 3;
        private static readonly SemaphoreSlim _processConcurrency = new SemaphoreSlim(_maxProcessConcurrency);

        public NamespaceCompatibilityChecker(
            ITransferUtility transferUtility,
            ILogger<NamespaceCompatibilityChecker> logger,
            IOptions<AnalyzerConfiguration> configuration
            )
        {
            _logger = logger;
            _transferUtility = transferUtility;
            _configuration = configuration;
        }

        public Task<NamespaceDetails> GetNamespaceDetails(string Namespace)
        {
            var isNotRunning = _resultsDict.TryAdd(
                    Namespace,
                    new NamespaceResult
                    {
                        TaskCompletionSource = new TaskCompletionSource<NamespaceDetails>()
                    });
            if (isNotRunning)
            {
                Process(Namespace);
            }
            _resultsDict.TryGetValue(Namespace, out var NamespaceResult);

            _logger.LogInformation("Checking compatibility for namespace: {0}", Namespace);
            return NamespaceResult.TaskCompletionSource.Task;
        }

        private void Process(string Namespace)
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
                if (result.NamespaceDetails.Package.Name == null || result.NamespaceDetails.Package.Name.Trim().ToLower() != Namespace.Trim().ToLower())
                {
                    throw new Exception(); //To be fill
                }
                if (_resultsDict.TryGetValue(Namespace, out var namespaceResult))
                {
                    namespaceResult.TaskCompletionSource.SetResult(result.NamespaceDetails);
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

        public PackageSourceType GetCompatibilityCheckerType()
        {
            return PackageSourceType.SDK;
        }

        public class NamespaceDetails
        {
            public PackageDetails Package { get; set; }
            public string[] Namespaces { get; set; }
            public string[] Assemblies { get; set; }
        }

        private class PackageFromS3
        {
            public NamespaceDetails NamespaceDetails { get; set; }
        }

        private class NamespaceResult
        {
            public TaskCompletionSource<NamespaceDetails> TaskCompletionSource { get; set; }
        }
    }
}
