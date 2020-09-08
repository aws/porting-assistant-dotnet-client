using Amazon;
using Amazon.S3.Transfer;
using PortingAssistant.ApiAnalysis;
using PortingAssistant.NuGet;
using PortingAssistant.Model;
using PortingAssistant.InternalNuGetChecker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace PortingAssistant
{
    public static class DependencyInjection
    {
        public static void AddAssessment(this IServiceCollection serviceCollection, IConfiguration cacheConfig)
        {
            serviceCollection.AddSingleton<IAssessmentHandler, AssessmentHandler>();
            serviceCollection.AddSingleton<IPortingAssistantInternalNuGetCompatibilityHandler, PortingAssistantInternalNuGetCompatibilityHandler>();
            serviceCollection.Configure<AnalyzerConfiguration>(cacheConfig);
            serviceCollection.AddSingleton<IPortingAssistantNuGetHandler, PortingAssistantNuGetHandler>();
            serviceCollection.AddSingleton<IPortingAssistantApiAnalysisHandler, PortingAssistantApiAnalysisHandler>();
            serviceCollection.AddSingleton<IPortingAssistantRecommendationHandler, RecommendationChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, InternalPackagesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, ExternalPackagesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, NamespacesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, PortabilityAnalyzerCompatibilityChecker>();
            serviceCollection.AddSingleton<ITransferUtility, TransferUtility>();
        }
    }
}
