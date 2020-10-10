
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using PortingAssistant.Client.Client.Reports;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Client
{
    public class PortingAssistantBuilder
    {

        private ServiceCollection ServiceCollection;
        private readonly IPortingAssistantClient PortingAssistantClient;
        private readonly IReportExporter ReportExporter;
        private readonly AnalyzerConfiguration Configuration;
        private readonly ILogger Logger;
        public IPortingAssistantClient GetPortingAssistant()
        {
            return PortingAssistantClient;
        }

        public IReportExporter GetReportExporter()
        {
            return ReportExporter;
        }

        private PortingAssistantBuilder(ILogger logger)
        {
            this.Logger = logger;
            this.Configuration = new AnalyzerConfiguration()
            {
                UseDataStoreSettings = true,
                UseInternalNuGetServer = false,
                DataStoreSettings = new DataStoreSettings
                {
                    HttpsEndpoint = "https://s3.us-west-2.amazonaws.com/aws.portingassistant.dotnet.datastore/",
                    S3Endpoint = "aws.portingassistant.dotnet.datastore"
                },
                InternalNuGetServerSettings = new NuGetServerSettings
                {
                    NugetServerEndpoint = "NugetServerEndpoint",
                }
            };
            ConfigureServices(Configuration);
            var services = ServiceCollection.BuildServiceProvider();
            this.PortingAssistantClient = services.GetService<IPortingAssistantClient>();
            this.ReportExporter = services.GetService<IReportExporter>();
        }

        public static PortingAssistantBuilder Build(ILogger logger = null)
        {
            return new PortingAssistantBuilder(logger);
        }

        private void ConfigureServices(AnalyzerConfiguration configaration)
        {
            ServiceCollection = new ServiceCollection();
            ServiceCollection.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger: Logger, dispose: false));
            ServiceCollection.AddAssessment(configaration);
            ServiceCollection.AddSingleton<IReportExporter, ReportExporter>();
            ServiceCollection.AddOptions();
        }
    }

}
