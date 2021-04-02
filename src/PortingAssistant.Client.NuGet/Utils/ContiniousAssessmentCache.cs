using Newtonsoft.Json;
using PortingAssistant.Client.Model;
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace PortingAssistant.Client.NuGet.Utils
{
    class ContiniousAssessmentCache
    {
        private string _tempSolutionDirectory;

        public ContiniousAssessmentCache(string pathToSolution)
        {
            string solutionId;
            using (var sha = new SHA256Managed())
            {
                byte[] textData = System.Text.Encoding.UTF8.GetBytes(pathToSolution);
                byte[] hash = sha.ComputeHash(textData);
                solutionId = BitConverter.ToString(hash);
            }
            _tempSolutionDirectory = Path.Combine(Path.GetTempPath(), solutionId);
            _tempSolutionDirectory = _tempSolutionDirectory.Replace("-", "");
        }
        public bool IsPackageInFile(PackageVersionPair packageVersion)
        {
            string fileName = string.Join("-", packageVersion.PackageId, packageVersion.PackageSourceType.ToString());
            fileName = string.Join(".", fileName, "gz");
            string filePath = Path.Combine(_tempSolutionDirectory, fileName);
            return File.Exists(filePath);
        }
        public async void CachePackageDetailsToFile(PackageVersionPair packageVersion, Task<PackageDetails> packageDetailTask)
        {
            var packageDetail  = await packageDetailTask;
            string fileName = string.Join("-", packageVersion.PackageId, packageVersion.PackageSourceType.ToString());
            fileName = string.Join(".", fileName, "gz");
            if(!Directory.Exists(_tempSolutionDirectory))
            {
                Directory.CreateDirectory(_tempSolutionDirectory);
            }
            string filePath = Path.Combine(_tempSolutionDirectory, fileName);
            var data = JsonConvert.SerializeObject(packageDetail);
            using FileStream compressedFileStream = File.OpenWrite(filePath);
            using var gzipStream = new GZipStream(compressedFileStream, CompressionMode.Compress);
            using var streamWriter = new StreamWriter(gzipStream);
            await streamWriter.WriteAsync(data);
        }
        public async Task<PackageDetails> GetPackageDetailFromFile(PackageVersionPair packageVersion)
        {
            string fileName = string.Join("-", packageVersion.PackageId, packageVersion.PackageSourceType.ToString());
            fileName = string.Join(".", fileName, "gz");
            string filePath = Path.Combine(_tempSolutionDirectory, fileName);
            using FileStream compressedFileStream = File.OpenRead(filePath);
            using var gzipStream = new GZipStream(compressedFileStream, CompressionMode.Decompress);
            using var streamReader = new StreamReader(gzipStream);
            var data = JsonConvert.DeserializeObject<PackageDetails>(await streamReader.ReadToEndAsync());
            return data;
        }
    }
}
