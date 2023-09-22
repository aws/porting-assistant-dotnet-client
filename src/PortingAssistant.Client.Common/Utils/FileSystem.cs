using System.IO;

namespace PortingAssistant.Client.Common.Utils
{
    class FileSystem : IFileSystem
    {
        public string GetTempPath()
        {
            return Path.GetTempPath();
        }
        public bool DirectoryExists(string directory)
        {
            return Directory.Exists(directory);
        }
        public bool FileExists(string file)
        {
            return File.Exists(file);
        }
        public DirectoryInfo CreateDirectory(string directory)
        {
            return Directory.CreateDirectory(directory);
        }
        public Stream FileOpenWrite(string filePath)
        {
            return File.OpenWrite(filePath);
        }
        public Stream FileOpenRead(string filePath)
        {
            return File.OpenRead(filePath);
        }
    }
}
