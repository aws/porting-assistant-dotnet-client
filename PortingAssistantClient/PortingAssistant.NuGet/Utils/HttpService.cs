﻿using System;
using System.Net.Http;
using PortingAssistant.NuGet.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace PortingAssistant.NuGet.Utils
{
    public class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;

        public HttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Stream> DownloadS3FileAsync(string fileToDownload)
        {
            return await _httpClient.GetStreamAsync(fileToDownload);
        }
    }
}
