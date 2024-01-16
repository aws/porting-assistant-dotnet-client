using Amazon;
using Amazon.S3;
using PortingAssistant.Compatibility.Common.Interface;
using Amazon.S3.Model;

namespace PortingAssistant.Compatibility.Common.Utils
{
    public class RegionalDatastoreService : IRegionalDatastoreService
    {
        private readonly IHttpService _httpService;
        private readonly AmazonS3Client _s3Client;
        private readonly bool _isLambdaEnvSetup;
        private readonly string _regionaS3BucketName;

        public RegionalDatastoreService(IHttpService httpService)
        {
            _httpService = httpService;
            string region = Environment.GetEnvironmentVariable("AWS_REGION");
            string stage = Environment.GetEnvironmentVariable("stage");

            if (!string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(stage)) 
            {
                _isLambdaEnvSetup = true;
                _regionaS3BucketName = $"portingassistant-datastore-{stage}-{region}";
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
                if (isRegionalCall && _isLambdaEnvSetup)
                {
                    GetObjectRequest request = new GetObjectRequest
                    {
                        BucketName = _regionaS3BucketName,
                        Key = fileToDownload
                    };
                    using (GetObjectResponse response = await _s3Client.GetObjectAsync(request))
                    {
                        Console.WriteLine($"Downloaded {fileToDownload} from " + _regionaS3BucketName);
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
                Console.WriteLine($"fail to download {fileToDownload}. " + ex.Message);
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
