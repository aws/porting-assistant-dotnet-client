namespace PortingAssistant.Compatibility.Common.Interface
{
    public interface IRegionalDatastoreService
    {
        public Task<string?> DownloadRegionalS3FileAsync(string fileToDownload, bool isRegionalCall = false, bool compressed = true);

        public Task<HashSet<string>> ListRegionalNamespacesObjectAsync(bool isRegionalCall = false);

        public Task<Stream> DownloadGitHubFileAsync(string fileToDownload);

        public Task<string?> ParseS3ObjectToString(Stream stream, string fileToDownload, bool compressed);
    }
}
