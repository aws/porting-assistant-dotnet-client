using System;
using System.Collections.Generic;

namespace PortingAssistant.Model
{
    public class AnalyzerConfiguration
    {
        public Settings Settings { get; set; }

        public bool UseDataStoreSettings { get; set; }
        public bool UseInternalNuGetServer { get; set; }

        public DataStoreSettings DataStoreSettings { get; set; }

        public NuGetServerSettings InternalNuGetServerSettings { get; set; } //optional

    }
}
