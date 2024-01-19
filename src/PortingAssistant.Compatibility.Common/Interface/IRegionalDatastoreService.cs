namespace PortingAssistant.Compatibility.Common.Interface
{
    public interface IRegionalDatastoreService
    {
        public Task<Stream?> DownloadRegionalS3FileAsync(string fileToDownload, bool isRegionalCall = false);

        public Task<HashSet<string>> ListRegionalNamespacesObjectAsync(bool isRegionalCall = false);

        public Task<Stream> DownloadGitHubFileAsync(string fileToDownload);
    }
}
