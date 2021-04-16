using Newtonsoft.Json;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.NuGet.Interfaces;
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static PortingAssistant.Client.NuGet.ExternalCompatibilityChecker;

namespace PortingAssistant.Client.NuGet.Utils
{
    class PackageDetailsManager
    {
        private string _tempSolutionDirectory;
        private IFileSystem _fileSystem;

        public PackageDetailsManager(string pathToSolution)
        {
            _fileSystem = new FileSystem();
            if (pathToSolution != null)
            {
                string solutionId;
                using (var sha = new SHA256Managed())
                {
                    byte[] textData = System.Text.Encoding.UTF8.GetBytes(pathToSolution);
                    byte[] hash = sha.ComputeHash(textData);
                    solutionId = BitConverter.ToString(hash);
                }
                _tempSolutionDirectory = Path.Combine(_fileSystem.GetTempPath(), solutionId);
                _tempSolutionDirectory = _tempSolutionDirectory.Replace("-", "");
            }
        }
        public bool IsPackageInFile(string fileToDownload)
        {
            string filePath = Path.Combine(_tempSolutionDirectory, fileToDownload);
            return _fileSystem.FileExists(filePath);
        }
        public async Task<PackageDetails> GetPackageDetailFromS3(string fileToDownload, IHttpService httpService)
        {
            using var stream = await httpService.DownloadS3FileAsync(fileToDownload);
            using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            using var streamReader = new StreamReader(gzipStream);
            var data = JsonConvert.DeserializeObject<PackageFromS3>(streamReader.ReadToEnd());
            var packageDetails = data.Package ?? data.Namespaces;
            return packageDetails;
        }
        public async void CachePackageDetailsToFile(string fileName, PackageDetails packageDetail)
        {
            if(!_fileSystem.DirectoryExists(_tempSolutionDirectory))
            {
                _fileSystem.CreateDirectory(_tempSolutionDirectory);
            }
            string filePath = Path.Combine(_tempSolutionDirectory, fileName);
            var data = JsonConvert.SerializeObject(packageDetail);
            using Stream compressedFileStream = _fileSystem.FileOpenWrite(filePath);
            using var gzipStream = new GZipStream(compressedFileStream, CompressionMode.Compress);
            using var streamWriter = new StreamWriter(gzipStream);
            await streamWriter.WriteAsync(data);
        }
        public async Task<PackageDetails> GetPackageDetailFromFile(string fileToDownload)
        {
            string filePath = Path.Combine(_tempSolutionDirectory, fileToDownload);
            using Stream compressedFileStream = _fileSystem.FileOpenRead(filePath);
            using var gzipStream = new GZipStream(compressedFileStream, CompressionMode.Decompress);
            using var streamReader = new StreamReader(gzipStream);
            var data = JsonConvert.DeserializeObject<PackageDetails>(await streamReader.ReadToEndAsync());
            return data;
        }
    }
}
