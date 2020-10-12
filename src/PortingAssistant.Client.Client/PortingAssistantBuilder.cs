
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

        private PortingAssistantBuilder(AnalyzerConfiguration configuration, ILogger logger)
        {
            this.Logger = logger;
            this.Configuration = configuration;
            ConfigureServices();
            var services = ServiceCollection.BuildServiceProvider();
            this.PortingAssistantClient = services.GetService<IPortingAssistantClient>();
            this.ReportExporter = services.GetService<IReportExporter>();
        }

        public static PortingAssistantBuilder Build(AnalyzerConfiguration configuration, ILogger logger = null)
        {
            return new PortingAssistantBuilder(configuration, logger);
        }

        private void ConfigureServices()
        {
            ServiceCollection = new ServiceCollection();
            ServiceCollection.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger: Logger, dispose: false));
            ServiceCollection.AddAssessment(Configuration);
            ServiceCollection.AddSingleton<IReportExporter, ReportExporter>();
            ServiceCollection.AddOptions();
        }
    }

}
