using System;
using System.Collections.Generic;
using Amazon.Runtime;

namespace PortingAssistant.Client.Telemetry
{
    public class TelemetryRequest : AmazonWebServiceRequest
    {
        public string Content;
        public readonly RequestMetadata RequestMetadata;

        public TelemetryRequest(string serviceName, string content)
        {
            Content = content;
            RequestMetadata = new RequestMetadata(serviceName);
        }
    }

    public class RequestMetadata
    {
        public string Service;
        public readonly string Version = "1.0";
        public readonly string Token = "12345678";
        public readonly string Created = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        public RequestMetadata(string serviceName)
        {
            Service = serviceName;
        }
    }
}
