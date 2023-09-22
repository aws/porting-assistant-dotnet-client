using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PortingAssistant.Client.Common.Utils
{
    public interface IFileSystem
    {
        public string GetTempPath();
        public bool DirectoryExists(string directory);
        public bool FileExists(string file);
        public DirectoryInfo CreateDirectory(string directory);
        public Stream FileOpenWrite(string filePath);
        public Stream FileOpenRead(string filePath);
    }
}
