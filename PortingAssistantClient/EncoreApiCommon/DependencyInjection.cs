using EncoreApiAnalysis;
using EncoreApiCommon.Services;
using EncoreAssessment;
using EncoreCache;
using EncorePrivateCompatibilityCheck;
using EncoreCommon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EncorePortingProjectFile;
using EncorePorting;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;
using Amazon;

namespace EncoreApiCommon
{
    public static class DependencyInjection
    {
        public static void AddAssessment(this IServiceCollection serviceCollection, IConfiguration cacheConfig)
        {
            serviceCollection.AddSingleton<IAssessmentService, AssessmentService>();
            serviceCollection.AddSingleton<IPortingService, PortingService>();
            serviceCollection.AddSingleton<IAssessmentHandler, AssessmentHandler>();
            serviceCollection.AddSingleton<IEncoreInternalCheckCompatibilityHandler, EncoreInternalCheckCompatibilityHandler>();
            serviceCollection.Configure<EndpointOptions>(cacheConfig);
            serviceCollection.AddSingleton<IEncoreCacheHandler, EncoreCacheHandler>();
            serviceCollection.AddSingleton<IEncoreApiAnalysisHandler, EncoreApiAnalysisHandler>();
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
