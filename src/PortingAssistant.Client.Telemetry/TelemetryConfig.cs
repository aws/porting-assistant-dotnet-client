
using Amazon.Runtime;
using Amazon.Util.Internal;

namespace PortingAssistant.Client.Telemetry
{
    public class TelemetryConfig : ClientConfig
    {
        private static readonly string userAgentString =
            InternalSDKUtils.BuildUserAgentString("3.5.0.9");
        public TelemetryConfig()
        {
            AuthenticationServiceName = "execute-api";
        }
        public override string RegionEndpointServiceName => "encore";
        public override string ServiceVersion => "";
        public override string UserAgent => userAgentString;
    }
}
