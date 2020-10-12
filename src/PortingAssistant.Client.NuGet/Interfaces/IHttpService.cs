using System;
using System.IO;
using System.Threading.Tasks;

namespace PortingAssistant.Client.NuGet.Interfaces
{
    public interface IHttpService
    {
        public Task<Stream> DownloadS3FileAsync(string fileToDownload);
        public Task<Stream> DownloadGitHubFileAsync(string fileToDownload);
    }
}
