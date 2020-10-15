using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortingAssistant.Client.Client.Reports;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Client
{
    public class PortingAssistantBuilder
    {

        private ServiceCollection ServiceCollection;
        private readonly IPortingAssistantClient PortingAssistantClient;
        private readonly IReportExporter ReportExporter;
        private readonly PortingAssistantConfiguration Configuration;
        private readonly Action<ILoggingBuilder> LogConfiguration;
        public IPortingAssistantClient GetPortingAssistant()
        {
            return PortingAssistantClient;
        }

        public IReportExporter GetReportExporter()
        {
            return ReportExporter;
        }

        private PortingAssistantBuilder(PortingAssistantConfiguration configuration, Action<ILoggingBuilder> logConfiguration)
        {
            this.LogConfiguration = logConfiguration;
            this.Configuration = configuration;
            ConfigureServices();
            var services = ServiceCollection.BuildServiceProvider();
            this.PortingAssistantClient = services.GetService<IPortingAssistantClient>();
            this.ReportExporter = services.GetService<IReportExporter>();
        }

        public static PortingAssistantBuilder Build(PortingAssistantConfiguration configuration, Action<ILoggingBuilder> logConfiguration = null)
        {
            if (logConfiguration == null)
            {
                logConfiguration = (config) => config.AddConsole();
            }
            return new PortingAssistantBuilder(configuration, logConfiguration);
        }

        private void ConfigureServices()
        {
            ServiceCollection = new ServiceCollection();
            ServiceCollection.AddLogging(LogConfiguration);
            ServiceCollection.AddAssessment(Configuration);
            ServiceCollection.AddSingleton<IReportExporter, ReportExporter>();
            ServiceCollection.AddOptions();
        }
    }

}
