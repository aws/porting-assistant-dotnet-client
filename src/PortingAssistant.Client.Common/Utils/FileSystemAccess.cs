using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PortingAssistant.Client.Common.Utils
{
    public static class FileSystemAccess
    {
        private static string[] fileTypesToCheck = { ".csproj", ".cs" };

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

        private static bool CanWriteFile(string path)
        {
            try
            {
                using var fs = File.Open(path, FileMode.Open);
                return true;
            }
            catch (System.UnauthorizedAccessException e)
            {
                return false;
            }
        }

        private static bool CanWriteToDirectory(string path)
        {
            try
            {
                using FileStream fs = File.Create(
                    Path.Combine(path, Path.GetRandomFileName()), 
                    1, 
                    FileOptions.DeleteOnClose);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
