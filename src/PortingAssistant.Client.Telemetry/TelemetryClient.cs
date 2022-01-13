using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Auth;
using Amazon.Runtime.Internal.Transform;

namespace PortingAssistant.Client.Telemetry
{
    public class TelemetryClient : AmazonServiceClient
    {
        public TelemetryClient(TelemetryConfig config)
        : this(FallbackCredentialsFactory.GetCredentials(), config)
        {
        }
        public TelemetryClient(AWSCredentials credentials, TelemetryConfig config) : base(credentials, config)
        {
        }
        public TelemetryClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, TelemetryConfig config) : base(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, config)
        {
        }
        public TelemetryClient(string awsAccessKeyId, string awsSecretAccessKey, TelemetryConfig config) : base(awsAccessKeyId, awsSecretAccessKey, config)
        {
        }
        protected override AbstractAWSSigner CreateSigner()
        {
            return new AWS4Signer();
        }
        public Task<AmazonWebServiceResponse> SendAsync(TelemetryRequest request)
        {
            var options = new InvokeOptions();
            options.RequestMarshaller = TelemetryRequestMarshaller.Instance;
            options.ResponseUnmarshaller = TelemetryResponseUnmarshaller.Instance;
            return InvokeAsync<AmazonWebServiceResponse>(request, options, CancellationToken.None);
        }
    }

    internal class TelemetryRequestMarshaller : IMarshaller<IRequest, TelemetryRequest>, IMarshaller<IRequest, AmazonWebServiceRequest>
    {
        private TelemetryRequestMarshaller()
        {
        }
        public IRequest Marshall(AmazonWebServiceRequest input)
        {
            return Marshall((TelemetryRequest)input);
        }
        public IRequest Marshall(TelemetryRequest publicRequest)
        {
            IRequest request = new DefaultRequest(publicRequest, publicRequest.RequestMetadata.Service);
            request.Headers["Content-Type"] = "application/json";
            request.HttpMethod = "POST";
            request.ResourcePath = "/put-log-data";
            request.MarshallerVersion = 2;
            request.Content = System.Text.Encoding.UTF8.GetBytes(publicRequest.Content);
            return request;
        }

        public static TelemetryRequestMarshaller Instance = new TelemetryRequestMarshaller();
    }
    internal class TelemetryResponseUnmarshaller : JsonResponseUnmarshaller
    {
        private TelemetryResponseUnmarshaller()
        {
        }
        public override AmazonWebServiceResponse Unmarshall(JsonUnmarshallerContext context)
        => new AmazonWebServiceResponse();
        public override AmazonServiceException UnmarshallException(JsonUnmarshallerContext context, Exception innerException, HttpStatusCode statusCode)
        {
            var errorResponse = JsonErrorResponseUnmarshaller.GetInstance().Unmarshall(context);
            return new AmazonServiceException(errorResponse.Message, innerException, errorResponse.Type, errorResponse.Code, errorResponse.RequestId, statusCode);
        }
        public static readonly TelemetryResponseUnmarshaller Instance = new TelemetryResponseUnmarshaller();
    }
}