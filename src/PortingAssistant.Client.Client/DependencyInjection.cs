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
using Polly;
using System.Net.Http;
using Polly.Extensions.Http;

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
            serviceCollection.AddHttpClient("s3")
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetRetryPolicy());
            serviceCollection.AddHttpClient("github")
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetRetryPolicy());
        }

        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            Random jitterer = new Random();
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3,    // exponential back-off plus some jitter
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                  + TimeSpan.FromMilliseconds(jitterer.Next(0, 100))
                );
        }
    }
}
