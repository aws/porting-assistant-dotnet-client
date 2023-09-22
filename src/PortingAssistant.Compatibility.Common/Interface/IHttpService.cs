namespace PortingAssistant.Compatibility.Common.Interface
{
    public interface IHttpService
    {
        public Task<Stream?> DownloadS3FileAsync(string fileToDownload);

        public Task<HashSet<string>> ListNamespacesObjectAsync();

        public Task<Stream> DownloadGitHubFileAsync(string fileToDownload);
    }
}
