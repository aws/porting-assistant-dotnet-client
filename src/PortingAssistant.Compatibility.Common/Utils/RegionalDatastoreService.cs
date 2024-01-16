using Amazon;
using Amazon.S3;
using PortingAssistant.Compatibility.Common.Interface;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace PortingAssistant.Compatibility.Common.Utils
{
    public class RegionalDatastoreService : IRegionalDatastoreService
    {
        private readonly IHttpService _httpService;
        private readonly AmazonS3Client _s3Client;
        private readonly bool _isLambdaEnvSetup;
        private readonly string _regionaS3BucketName;
        private readonly ILogger<RegionalDatastoreService> _logger;

        public RegionalDatastoreService(
            IHttpService httpService,
            ILogger<RegionalDatastoreService> logger
            )
        {
            _httpService = httpService;
            _logger = logger;
            string region = Environment.GetEnvironmentVariable("AWS_REGION");
            string stage = Environment.GetEnvironmentVariable("stage");

            if (!string.IsNullOrEmpty(region) && (stage == Constants.BetaStageName || stage == Constants.GammaStageName || stage == Constants.ProdStageName)) 
            {
                _isLambdaEnvSetup = true;
                _regionaS3BucketName = stage == Constants.ProdStageName ? 
                    $"portingassistant-datastore-{region}" : $"portingassistant-datastore-{stage}-{region}"; 
                _s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(region));
            }
        }

        public async Task<Stream> DownloadGitHubFileAsync(string fileToDownload)
        {
            return await _httpService.DownloadGitHubFileAsync(fileToDownload);
        }

        public async Task<Stream?> DownloadRegionalS3FileAsync(string fileToDownload, bool isRegionalCall = false)
        {
            try 
            {
                _logger.LogInformation($"Downloading {fileToDownload} from regional S3 " + _regionaS3BucketName);
                if (isRegionalCall && _isLambdaEnvSetup)
                {
                    GetObjectRequest request = new GetObjectRequest
                    {
                        BucketName = _regionaS3BucketName,
                        Key = fileToDownload
                    };
                    using (GetObjectResponse response = await _s3Client.GetObjectAsync(request))
                    {
                        _logger.LogInformation($"Downloaded {fileToDownload} from " + _regionaS3BucketName);
                        return response.ResponseStream;
                    }

                }
                else
                {
                    return await _httpService.DownloadS3FileAsync(fileToDownload);
                }
            }
            catch (Exception ex) 
            {
                _logger.LogError($"fail to download {fileToDownload}. " + ex.Message);
                return null;
            }
            
        }

        // TODO: This method could be deprecated since sdk namespaces won't change after each feature release
        public Task<HashSet<string>> ListRegionalNamespacesObjectAsync(bool isRegionalCall = false)
        {
            return _httpService.ListNamespacesObjectAsync();
        }
    }
}
