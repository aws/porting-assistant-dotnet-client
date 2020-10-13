﻿using System;
using System.Net.Http;
using PortingAssistant.Client.NuGet.Interfaces;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.NuGet.Utils
{
    public class HttpService : IHttpService
    {
        private readonly HttpClient _S3httpClient;
        private readonly HttpClient _GitHubHttpClient;

        public HttpService(IHttpClientFactory httpClientFactory, IOptions<PortingAssistantConfiguration> options)
        {
            _S3httpClient = httpClientFactory.CreateClient();
            _S3httpClient.BaseAddress = new Uri(options.Value.DataStoreSettings.HttpsEndpoint);
            _GitHubHttpClient = httpClientFactory.CreateClient();
            _GitHubHttpClient.BaseAddress = new Uri(options.Value.DataStoreSettings.GitHubEndpoint);
        }

        public async Task<Stream> DownloadS3FileAsync(string fileToDownload)
        {
            return await _S3httpClient.GetStreamAsync(fileToDownload);
        }

        public async Task<Stream> DownloadGitHubFileAsync(string fileToDownload)
        {
            return await _GitHubHttpClient.GetStreamAsync(fileToDownload);
        }
    }
}
