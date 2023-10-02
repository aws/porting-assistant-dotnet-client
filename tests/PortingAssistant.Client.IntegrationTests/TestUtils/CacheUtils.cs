using CTA.FeatureDetection.Common;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace PortingAssistant.Client.IntegrationTests.TestUtils
{
    public class CacheUtils
    {
        public static void CleanupCacheFiles()
        {
            try
            {
                var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var cacheFolder = Path.Combine(roamingFolder, "Porting Assistant for .NET");
                
                var files = Directory.GetFiles(cacheFolder, "compatibility-checker-cache*");
                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    fi.Delete();
                }
            }
            catch (Exception ex)
            {
                Log.Logger.LogError(ex, "Failed to delete cache file");
            }
        }
    }
}
