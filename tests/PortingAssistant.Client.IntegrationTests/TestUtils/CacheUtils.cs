using CTA.FeatureDetection.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Client.IntegrationTests.TestUtils
{
    class CacheUtils
    {
        public static void CleanupCaches()
        {
            try
            {
                var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var cacheFolder = Path.Combine(roamingFolder, "Porting Assistant for .NET");
                //, $"compatibility-checker-cache-{cli.AssessmentType}.json");

                string[] files = Directory.GetFiles(cacheFolder, "compatibility-checker-cache");
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    fi.Delete();

                }
            }catch (Exception ex)
            {
                Log.Logger.LogError(ex, "fail to delete cache");

            }
        }
    }
}
