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
            serviceCollection.AddTransient<IPortingAssistantClient, PortingAssistantClient>();
            serviceCollection.AddTransient<IPortingAssistantInternalNuGetCompatibilityHandler, PortingAssistantInternalNuGetCompatibilityHandler>();
            serviceCollection.AddTransient<IPortingAssistantNuGetHandler, PortingAssistantNuGetHandler>();
            serviceCollection.AddTransient<IPortingAssistantAnalysisHandler, PortingAssistantAnalysisHandler>();
            serviceCollection.AddTransient<IPortingAssistantRecommendationHandler, PortingAssistantRecommendationHandler>();
            serviceCollection.AddTransient<ICompatibilityChecker, ExternalPackagesCompatibilityChecker>();
            serviceCollection.AddTransient<ICompatibilityChecker, SdkCompatibilityChecker>();
            serviceCollection.AddTransient<ICompatibilityChecker, PortabilityAnalyzerCompatibilityChecker>();
            serviceCollection.AddTransient<IPortingHandler, PortingHandler>();
            serviceCollection.AddTransient<IPortingProjectFileHandler, PortingProjectFileHandler>();
            serviceCollection.AddTransient<IHttpService, HttpService>();
            serviceCollection.AddHttpClient();
        }
    }
}
