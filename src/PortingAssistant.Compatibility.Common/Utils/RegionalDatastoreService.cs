using Amazon;
using Amazon.S3;
using PortingAssistant.Compatibility.Common.Interface;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System.Net;
using System.IO.Compression;
using System.Text;

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
            // This service is used by service and non-service clients. We use Console.WriteLine instead of private logger to write into CloudWatch
            _logger = logger;
            string region = Environment.GetEnvironmentVariable("AWS_REGION");
            string stage = Environment.GetEnvironmentVariable("stage");
            Console.WriteLine($"Read stage, region from environment: {stage}, {region}.");

            if (!string.IsNullOrEmpty(region) && (stage == Constants.BetaStageName || stage == Constants.GammaStageName || stage == Constants.ProdStageName)) 
            {
                _isLambdaEnvSetup = true;
                _regionaS3BucketName = stage == Constants.ProdStageName ?
                    $"portingassistant-datastore-{region}" : $"portingassistant-datastore-{stage}-{region}";
                Console.WriteLine($"Set S3 bucket name: {_regionaS3BucketName}.");
                _s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(region));
            }
        }

        public async Task<Stream> DownloadGitHubFileAsync(string fileToDownload)
        {
            return await _httpService.DownloadGitHubFileAsync(fileToDownload);
        }

        public async Task<string?> DownloadRegionalS3FileAsync(string fileToDownload, bool isRegionalCall = false, bool compressed = true)
        {
            try 
            {
                string? content = null;
                if (isRegionalCall && _isLambdaEnvSetup && await CheckObjectExistsAsync(fileToDownload))
                {
                    Console.WriteLine($"Downloading {fileToDownload} from regional S3 {_regionaS3BucketName}");
                    GetObjectRequest request = new GetObjectRequest
                    {
                        BucketName = _regionaS3BucketName,
                        Key = fileToDownload
                    };
                    using (GetObjectResponse response = await _s3Client.GetObjectAsync(request))
                    using (Stream responseStream = response.ResponseStream)
                    {
                        if (response.HttpStatusCode == HttpStatusCode.OK && responseStream != null && responseStream.CanRead)
                        {
                            Console.WriteLine($"Downloaded {fileToDownload} from {_regionaS3BucketName}.");
                            content = await ParseS3ObjectToString(responseStream, fileToDownload, compressed);
                        } 
                    }
                }
                if (content == null)
                {
                    Console.WriteLine($"Not a Lambda environment, or {fileToDownload} doesn't exist in {_regionaS3BucketName} or has null value, downloading file through Http client...");
                    content = await ParseS3ObjectToString(await _httpService.DownloadS3FileAsync(fileToDownload), fileToDownload, compressed);
                }
                return content;

            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Fail to download {fileToDownload}: " + ex.StackTrace);
                return null;
            }
            
        }

        // TODO: This method could be deprecated since sdk namespaces won't change after each feature release
        public Task<HashSet<string>> ListRegionalNamespacesObjectAsync(bool isRegionalCall = false)
        {
            return _httpService.ListNamespacesObjectAsync();
        }

        public async Task<bool> CheckObjectExistsAsync(string objectKey)
        {
            try
            {
                await _s3Client.GetObjectMetadataAsync(_regionaS3BucketName, objectKey);
                return true; // Object exists
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false; // Object does not exist
                }

                // Handle other exceptions
                throw;
            }
        }

        public async Task<string?> ParseS3ObjectToString(Stream stream, string fileToDownload, bool compressed) {
            if (stream == null)
            {
                return null;
            }
            string? content = null;
            if (!compressed)
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    content = await reader.ReadToEndAsync();
                }
            }
            else
            {
                using (var decompressionStream = new GZipStream(stream, CompressionMode.Decompress))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await decompressionStream.CopyToAsync(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        using (StreamReader reader = new StreamReader(memoryStream))
                        {
                            content = await reader.ReadToEndAsync();       
                        }
                    }
                }
            }
            Console.WriteLine($"Read object content for: {fileToDownload}");
            return content;
        }
    }
}
