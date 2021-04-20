using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Client.NuGet.Interfaces
{
    public interface IPackageDetailsManager
    {
        public string GetTempDirectory(string pathToSolution);
        public bool IsPackageInFile(string fileToDownload, string _tempSolutionDirectory);
        public Task<PackageDetails> GetPackageDetailFromS3(string fileToDownload, IHttpService httpService);
        public void CachePackageDetailsToFile(string fileName, PackageDetails packageDetail, string _tempSolutionDirectory);
        public Task<PackageDetails> GetPackageDetailFromFile(string fileToDownload, string _tempSolutionDirectory);
    }
}
