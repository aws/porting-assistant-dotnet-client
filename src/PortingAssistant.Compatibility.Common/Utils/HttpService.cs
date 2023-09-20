using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using System.Xml;

namespace PortingAssistant.Compatibility.Common.Utils
{
    public class HttpService : IHttpService
    {

        private readonly HttpClient _S3httpClient;
        private readonly string _S3EndPoint;
        private readonly HttpClient _GitHubHttpClient;

        public HttpService(IHttpClientFactory httpClientFactory, IOptions<CompatibilityCheckerConfiguration> options)
        {
            _S3httpClient = httpClientFactory.CreateClient("s3");
            _S3httpClient.BaseAddress = new Uri(options.Value.DataStoreSettings.HttpsEndpoint);
            _S3EndPoint = options.Value.DataStoreSettings.S3Endpoint;
            _GitHubHttpClient = httpClientFactory.CreateClient("github");
            _GitHubHttpClient.BaseAddress = new Uri(options.Value.DataStoreSettings.GitHubEndpoint);
        }

        public async Task<Stream?> DownloadS3FileAsync(string fileToDownload)
        {
            try
            {
                return await _S3httpClient.GetStreamAsync(fileToDownload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"fail to download {fileToDownload}. " + ex.Message);
                return null;
            }
        }

        public async Task<Stream> DownloadGitHubFileAsync(string fileToDownload)
        {
            return await _GitHubHttpClient.GetStreamAsync(fileToDownload);
        }



        public async Task<HashSet<string>> ListNamespacesObjectAsync()
        {
            HashSet<string> namespaces = new HashSet<string>();
            try
            {
                var unsigned = new AnonymousAWSCredentials();
                using (var client = new AmazonS3Client(unsigned, Amazon.RegionEndpoint.USWest2))
                {
                    ListObjectsV2Request request = new ListObjectsV2Request
                    {
                        BucketName = _S3EndPoint,
                        MaxKeys = 1000,
                        Prefix = "namespaces/"
                    };
                    ListObjectsV2Response response;
                    do
                    {
                        response = await client.ListObjectsV2Async(request);
                        foreach (var item in response.S3Objects)
                        {
                            if (item.Key.EndsWith(".json.gz"))
                            {
                                namespaces.Add(item.Key.Replace("namespaces/", "").Replace(".json.gz", "").ToLower());
                            }
                        }

                        request.ContinuationToken = response.NextContinuationToken;

                    } while (response.IsTruncated);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"fail to list all namespaces. " + ex.Message);

            }
            return namespaces;

        }
    }
}