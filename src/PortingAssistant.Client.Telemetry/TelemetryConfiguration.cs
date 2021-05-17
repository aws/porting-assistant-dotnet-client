using System;
using System.Collections.Generic;
using System.IO;

namespace PortingAssistantExtensionTelemetry.Model
{
    public class TelemetryConfiguration
    {
        public string InvokeUrl { get; set; }
        public string Region { get; set; }
        public string LogsPath { get; set; }
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public List<string> Suffix { get; set; }
        public string LogFilePath { get; set; }
        public string MetricsFilePath { get; set; }
    }
}
