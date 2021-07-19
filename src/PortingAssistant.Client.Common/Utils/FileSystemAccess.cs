using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PortingAssistant.Client.Common.Utils
{
    public static class FileSystemAccess
    {
        private static readonly string[] fileTypesToCheck = { ".csproj", ".cs" };

        /// <summary>
        /// Checks directory and all content for write access
        /// </summary>
        /// <param name="path">Directory path</param>
        /// <returns>List of items without write access</returns>
        public static List<string> CheckWriteAccessForDirectory(string path)
        {
            var result = (Directory.GetFiles(path)
                .Where(file => fileTypesToCheck.Contains(Path.GetExtension(file)))
                .Where(file => !CanWriteFile(file))).ToList();
            foreach (string subDirectory in Directory.GetDirectories(path))
            {
                if (!CanWriteToDirectory(subDirectory))
                {
                    result.Add(subDirectory);
                }
                result.AddRange(CheckWriteAccessForDirectory(subDirectory));
            }
            return result;
        }

        /// <summary>
        /// Checks csproj and at least one .cs file is writeable
        /// </summary>
        /// <param name="projectFilePath">Project file path</param>
        /// <returns>True if csproj and at least one .cs file is writeable</returns>
        public static bool CheckWriteAccessForProject(string projectFilePath)
        {
            return CanWriteFile(projectFilePath) &&
                 DirectoryHasWriteableCSharpFile(Path.GetDirectoryName(projectFilePath));
        }

        private static bool DirectoryHasWriteableCSharpFile(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                return false;
            }

            bool fileFound = Directory.GetFiles(directoryPath)
                .Where(file => Path.GetExtension(file) == ".cs")
                .Any(file => CanWriteFile(file));

            return fileFound || 
                Directory.GetDirectories(directoryPath).Any(subDirectory => DirectoryHasWriteableCSharpFile(subDirectory));
        }

        private static bool CanWriteFile(string filePath)
        {
            try
            {
                using var fs = File.Open(filePath, FileMode.Open, FileAccess.Write);
                return fs.CanWrite;
            }
            catch (System.UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static bool CanWriteToDirectory(string directoryPath)
        {
            try
            {
                using FileStream fs = File.Create(
                    Path.Combine(directoryPath, Path.GetRandomFileName()), 
                    1, 
                    FileOptions.DeleteOnClose);
                return true;
            }
            catch (System.UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
