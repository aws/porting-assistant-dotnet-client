using PortingAssistantApiAnalysis;
using PortingAssistantApiCommon.Services;
using PortingAssistantAssessment;
using PortingAssistantCache;
using PortingAssistantPrivateCompatibilityCheck;
using PortingAssistantCommon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortingAssistantPortingProjectFile;
using PortingAssistantPorting;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;
using Amazon;

namespace PortingAssistantApiCommon
{
    public static class DependencyInjection
    {
        public static void AddAssessment(this IServiceCollection serviceCollection, IConfiguration cacheConfig)
        {
            serviceCollection.AddSingleton<IAssessmentService, AssessmentService>();
            serviceCollection.AddSingleton<IPortingService, PortingService>();
            serviceCollection.AddSingleton<IAssessmentHandler, AssessmentHandler>();
            serviceCollection.AddSingleton<IPortingAssistantInternalCheckCompatibilityHandler, PortingAssistantInternalCheckCompatibilityHandler>();
            serviceCollection.Configure<EndpointOptions>(cacheConfig);
            serviceCollection.AddSingleton<IPortingAssistantCacheHandler, PortingAssistantCacheHandler>();
            serviceCollection.AddSingleton<IPortingAssistantApiAnalysisHandler, PortingAssistantApiAnalysisHandler>();
            serviceCollection.AddSingleton<ICompatibilityChecker, InternalPackagesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, ExternalPackagesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, PortabilityAnalyzerCompatibilityChecker>();
            serviceCollection.AddSingleton<IPortingProjectFileHandler, PortingProjectFileHandler>();
            serviceCollection.AddSingleton<IPortingHandler, PortingHandler>();
            serviceCollection.AddSingleton<ITransferUtility>(dep => new TransferUtility(
                dep.GetService<IOptions<EndpointOptions>>().Value.AwsAccessKey,
                dep.GetService<IOptions<EndpointOptions>>().Value.AwsSecretKey,
                RegionEndpoint.GetBySystemName(dep.GetService<IOptions<EndpointOptions>>().Value.Region)));
        }
    }
}
