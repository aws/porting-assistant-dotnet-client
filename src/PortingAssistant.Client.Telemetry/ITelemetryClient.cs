using System.Threading.Tasks;
using Amazon.Runtime;

namespace PortingAssistant.Client.Telemetry
{
    public interface ITelemetryClient
    {
        public Task<AmazonWebServiceResponse> SendAsync(TelemetryRequest request);
    }
}
