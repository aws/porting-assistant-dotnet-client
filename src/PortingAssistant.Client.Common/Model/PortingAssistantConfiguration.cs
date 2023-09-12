using System;
using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class PortingAssistantConfiguration
    {

        public PortingAssistantConfiguration()
        {
            this.UseDataStoreSettings = true;
            this.CacheExpirationHours = 24;
        }
    

        public bool UseDataStoreSettings { get; set; }
        public int CacheExpirationHours { get; set; }
        public string CompatibilityCheckerCacheFilePath { get; set; }
    

        public PortingAssistantConfiguration DeepCopy()
        {
            return new PortingAssistantConfiguration
            {
                UseDataStoreSettings = this.UseDataStoreSettings,
                CacheExpirationHours = this.CacheExpirationHours,
                CompatibilityCheckerCacheFilePath = this.CompatibilityCheckerCacheFilePath
            };
        }

        public PortingAssistantConfiguration DeepCopy(PortingAssistantConfiguration that)
        {
            that.UseDataStoreSettings = this.UseDataStoreSettings;
            that.CacheExpirationHours = this.CacheExpirationHours;
            that.CompatibilityCheckerCacheFilePath= this.CompatibilityCheckerCacheFilePath;
            return that;
        }
    }
}
