using System;
using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class AnalyzerConfiguration
    {
        public bool UseDataStoreSettings { get; set; }
        public bool UseInternalNuGetServer { get; set; }

        public DataStoreSettings DataStoreSettings { get; set; }

        public NuGetServerSettings InternalNuGetServerSettings { get; set; } //optional

        public AnalyzerConfiguration DeepCopy()
        {
            return new AnalyzerConfiguration
            {
                UseDataStoreSettings = this.UseDataStoreSettings,
                UseInternalNuGetServer = this.UseInternalNuGetServer,
                DataStoreSettings = this.DataStoreSettings.DeepCopy(),
                InternalNuGetServerSettings = this.InternalNuGetServerSettings.DeepCopy()
            };
        }

        public AnalyzerConfiguration DeepCopy(AnalyzerConfiguration that)
        {
            that.UseDataStoreSettings = this.UseDataStoreSettings;
            that.UseInternalNuGetServer = this.UseInternalNuGetServer;
            that.DataStoreSettings = this.DataStoreSettings.DeepCopy();
            that.InternalNuGetServerSettings = this.InternalNuGetServerSettings.DeepCopy();
            return that;
        }
    }
}
