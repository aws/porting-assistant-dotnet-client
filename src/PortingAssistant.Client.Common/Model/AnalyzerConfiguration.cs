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

    }
}
