using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Auth;
using Amazon.Runtime.Internal.Transform;
using PortingAssistantExtensionTelemetry.Model;

namespace PortingAssistant.Client.Telemetry
{
    public class TelemetryClient : AmazonServiceClient, ITelemetryClient
    {
        public TelemetryClient(TelemetryClientConfig config)
        : this(FallbackCredentialsFactory.GetCredentials(), config)
        {
        }
        public TelemetryClient(AWSCredentials credentials, TelemetryClientConfig config) : base(credentials, config)
        {
        }
        public TelemetryClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, TelemetryClientConfig config) : base(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, config)
        {
        }
        public TelemetryClient(string awsAccessKeyId, string awsSecretAccessKey, TelemetryClientConfig config) : base(awsAccessKeyId, awsSecretAccessKey, config)
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

    public class TelemetryClientFactory
    {
        public static bool TryGetClient(string profile, TelemetryConfiguration config, out ITelemetryClient client)
        {
            client = null;
            var chain = new CredentialProfileStoreChain();
            if (chain.TryGetAWSCredentials(profile, out AWSCredentials awsCredentials))
            {
                client = new TelemetryClient(awsCredentials, new TelemetryClientConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(config.Region),
                    MaxErrorRetry = 2,
                    ServiceURL = config.InvokeUrl,
                });
                return true;
            }
            return false;
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