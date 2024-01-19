using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;

namespace PortingAssistant.Compatibility.Core
{
    // The CompatibilityCheckerRecommendationActionHandler checks and gets recommendation action file details ("namespace.json") from the datastore, if any.
    public class CompatibilityCheckerRecommendationActionHandler : ICompatibilityCheckerRecommendationActionHandler
    {
        private readonly IRegionalDatastoreService _regionalDatastoreService;
        private const string _recommendationFileSuffix = ".json";
        private ILogger _logger;
        public PackageSourceType CompatibilityCheckerType => PackageSourceType.RECOMMENDATION;


        public CompatibilityCheckerRecommendationActionHandler(
            IRegionalDatastoreService regionalDatastoreService,
            ILogger<CompatibilityCheckerRecommendationActionHandler> logger
            )
        {
            _regionalDatastoreService = regionalDatastoreService;
            _logger = logger;
        }

        public async Task<Dictionary<string, RecommendationActionFileDetails>> GetRecommendationActionFileAsync(
             IEnumerable<string> namespaces)
        {
            // Namespace RecommendationActionFileDetails dictionary
            var recommendationActionDetailsNamespaceDict = new Dictionary<string, RecommendationActionFileDetails>();

            foreach (var namespaceName in namespaces)
            {
                string fileToDownload = namespaceName.ToLower() + _recommendationFileSuffix;
                var recommendationDownloadPath = Path.Combine("recommendationsync", "recommendation",
                    fileToDownload);

                Stream? stream = null;
                try
                {
                    stream = await _regionalDatastoreService.DownloadRegionalS3FileAsync(recommendationDownloadPath, isRegionalCall: true);
                    using var streamReader = new StreamReader(stream);
                    var recommendationFromS3 = JsonConvert.DeserializeObject<RecommendationActionFileDetails>(await streamReader.ReadToEndAsync());
                    recommendationActionDetailsNamespaceDict.Add(namespaceName, recommendationFromS3);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404"))
                    {
                        _logger.LogInformation($"Encountered {ex.GetType()} while downloading and parsing {fileToDownload} " +
                                               $"from {CompatibilityCheckerType}, but it was ignored. " +
                                               $"Details: {ex.Message}.");
                        // filter all 404 errors
                        ex = null;
                    }
                    else
                    {
                        _logger.LogError($"Failed when downloading and parsing {fileToDownload} from {CompatibilityCheckerType}, {ex}");
                    }
                    recommendationActionDetailsNamespaceDict.Add(namespaceName, null);
                }

            }

            return recommendationActionDetailsNamespaceDict;
        }
    }
}