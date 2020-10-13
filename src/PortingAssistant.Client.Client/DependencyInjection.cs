using Amazon.S3.Transfer;
using PortingAssistant.Client.Analysis;
using PortingAssistant.Client.NuGet;
using PortingAssistant.Client.Model;
using PortingAssistant.Client.NuGet.InternalNuGet;
using Microsoft.Extensions.DependencyInjection;
using PortingAssistant.Client.Porting;
using PortingAssistant.Client.PortingProjectFile;
using PortingAssistant.Client.NuGet.Interfaces;
using PortingAssistant.Client.NuGet.Utils;
using System;
using Microsoft.Extensions.Options;

namespace PortingAssistant.Client.Client
{
    public static class DependencyInjection
    {
        public static void AddAssessment(this IServiceCollection serviceCollection, PortingAssistantConfiguration cacheConfig)
        {
            serviceCollection.Configure<PortingAssistantConfiguration>(config => cacheConfig.DeepCopy(config));
            serviceCollection.AddSingleton<IPortingAssistantClient, PortingAssistantClient>();
            serviceCollection.AddSingleton<IPortingAssistantInternalNuGetCompatibilityHandler, PortingAssistantInternalNuGetCompatibilityHandler>();
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
                client.BaseAddress = new Uri(services.GetService<IOptions<PortingAssistantConfiguration>>().Value.DataStoreSettings.HttpsEndpoint);
            });
        }
    }
}
