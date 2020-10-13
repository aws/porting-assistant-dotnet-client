using System;
using System.Collections.Generic;

namespace PortingAssistant.Client.Model
{
    public class PortingAssistantConfiguration
    {

        public PortingAssistantConfiguration()
        {
            this.UseDataStoreSettings = true;
            this.UseInternalNuGetServer = false;
            this.DataStoreSettings = new DataStoreSettings
            {
                HttpsEndpoint = "https://s3.us-west-2.amazonaws.com/aws.portingassistant.dotnet.datastore/",
                S3Endpoint = "aws.portingassistant.dotnet.datastore"
            };
            this.InternalNuGetServerSettings = new NuGetServerSettings
            {
                NugetServerEndpoint = "NugetServerEndpoint",
            };
        }

        public bool UseDataStoreSettings { get; set; }
        public bool UseInternalNuGetServer { get; set; }

        public DataStoreSettings DataStoreSettings { get; set; }

        public NuGetServerSettings InternalNuGetServerSettings { get; set; } //optional

        public PortingAssistantConfiguration DeepCopy()
        {
            return new PortingAssistantConfiguration
            {
                UseDataStoreSettings = this.UseDataStoreSettings,
                UseInternalNuGetServer = this.UseInternalNuGetServer,
                DataStoreSettings = this.DataStoreSettings.DeepCopy(),
                InternalNuGetServerSettings = this.InternalNuGetServerSettings.DeepCopy()
            };
        }

        public PortingAssistantConfiguration DeepCopy(PortingAssistantConfiguration that)
        {
            that.UseDataStoreSettings = this.UseDataStoreSettings;
            that.UseInternalNuGetServer = this.UseInternalNuGetServer;
            that.DataStoreSettings = this.DataStoreSettings.DeepCopy();
            that.InternalNuGetServerSettings = this.InternalNuGetServerSettings.DeepCopy();
            return that;
        }
    }
}
