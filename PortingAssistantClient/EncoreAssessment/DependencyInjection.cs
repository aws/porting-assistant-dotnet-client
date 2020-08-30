using Amazon;
using Amazon.S3.Transfer;
using EncoreApiAnalysis;
using EncoreCache;
using EncoreCommon;
using EncorePrivateCompatibilityCheck;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace EncoreAssessment
{
    public static class DependencyInjection
    {
        public static void AddAssessment(this IServiceCollection serviceCollection, IConfiguration cacheConfig)
        {
            serviceCollection.AddSingleton<IAssessmentHandler, AssessmentHandler>();
            serviceCollection.AddSingleton<IEncoreInternalCheckCompatibilityHandler, EncoreInternalCheckCompatibilityHandler>();
            serviceCollection.Configure<EndpointOptions>(cacheConfig);
            serviceCollection.AddSingleton<IEncoreCacheHandler, EncoreCacheHandler>();
            serviceCollection.AddSingleton<IEncoreApiAnalysisHandler, EncoreApiAnalysisHandler>();
            serviceCollection.AddSingleton<ICompatibilityChecker, InternalPackagesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, ExternalPackagesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, PortabilityAnalyzerCompatibilityChecker>();
            serviceCollection.AddSingleton<ITransferUtility>(dep => new TransferUtility(
                dep.GetService<IOptions<EndpointOptions>>().Value.AwsAccessKey,
                dep.GetService<IOptions<EndpointOptions>>().Value.AwsSecretKey,
                RegionEndpoint.GetBySystemName(dep.GetService<IOptions<EndpointOptions>>().Value.Region)));
        }
    }
}
