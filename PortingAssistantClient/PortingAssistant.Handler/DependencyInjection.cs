using Amazon.S3.Transfer;
using PortingAssistant.ApiAnalysis;
using PortingAssistant.NuGet;
using PortingAssistant.Model;
using PortingAssistant.NuGet.InternalNuGet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortingAssistant.Porting;
using PortingAssistant.PortingProjectFile;

namespace PortingAssistant.Handler
{
    public static class DependencyInjection
    {
        public static void AddAssessment(this IServiceCollection serviceCollection, IConfiguration cacheConfig)
        {
            serviceCollection.AddSingleton<IPortingAssistantHandler, PortingAssistantHandler>();
            serviceCollection.AddSingleton<IPortingAssistantInternalNuGetCompatibilityHandler, PortingAssistantInternalNuGetCompatibilityHandler>();
            serviceCollection.Configure<AnalyzerConfiguration>(cacheConfig);
            serviceCollection.AddSingleton<IPortingAssistantNuGetHandler, PortingAssistantNuGetHandler>();
            serviceCollection.AddSingleton<IPortingAssistantApiAnalysisHandler, PortingAssistantApiAnalysisHandler>();
            serviceCollection.AddSingleton<IPortingAssistantRecommendationHandler, PortingAssistantRecommendationHandler>();
            serviceCollection.AddSingleton<ICompatibilityChecker, InternalPackagesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, ExternalPackagesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, NamespacesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, PortabilityAnalyzerCompatibilityChecker>();
            serviceCollection.AddSingleton<IPortingHandler, PortingHandler>();
            serviceCollection.AddSingleton<IPortingProjectFileHandler, PortingProjectFileHandler>();
            serviceCollection.AddSingleton<ITransferUtility, TransferUtility>();
        }
    }
}
