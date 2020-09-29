using Amazon.S3.Transfer;
using PortingAssistant.Analysis;
using PortingAssistant.NuGet;
using PortingAssistant.Model;
using PortingAssistant.NuGet.InternalNuGet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortingAssistant.Porting;
using PortingAssistant.PortingProjectFile;
using PortingAssistant.NuGet.Interfaces;
using PortingAssistant.NuGet.Utils;
using System;
using Microsoft.Extensions.Options;

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
            serviceCollection.AddSingleton<IPortingAssistantAnalysisHandler, PortingAssistantAnalysisHandler>();
            serviceCollection.AddSingleton<IPortingAssistantRecommendationHandler, PortingAssistantRecommendationHandler>();
            serviceCollection.AddSingleton<ICompatibilityChecker, InternalPackagesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, ExternalPackagesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, SdkCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, PortabilityAnalyzerCompatibilityChecker>();
            serviceCollection.AddSingleton<IPortingHandler, PortingHandler>();
            serviceCollection.AddSingleton<IPortingProjectFileHandler, PortingProjectFileHandler>();
            serviceCollection.AddSingleton<ITransferUtility, TransferUtility>();
            serviceCollection.AddHttpClient<IHttpService, HttpService>(client =>
            {
                var services = serviceCollection.BuildServiceProvider();
                client.BaseAddress = new Uri(services.GetService<IOptions<AnalyzerConfiguration>>().Value.DataStoreSettings.HttpsEndpoint);
            });
        }
    }
}
