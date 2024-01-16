using PortingAssistant.Client.Analysis;
using PortingAssistant.Client.Model;
using Microsoft.Extensions.DependencyInjection;
using PortingAssistant.Client.Porting;
using PortingAssistant.Client.PortingProjectFile;
using System;
using Polly;
using System.Net.Http;
using Polly.Extensions.Http;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Core;

namespace PortingAssistant.Client.Client
{
    public static class DependencyInjection
    {
        public static void AddAssessment(this IServiceCollection serviceCollection, PortingAssistantConfiguration cacheConfig)
        {
            serviceCollection.Configure<PortingAssistantConfiguration>(config => cacheConfig.DeepCopy(config));
            serviceCollection.AddTransient<IPortingAssistantClient, PortingAssistantClient>();
            serviceCollection.AddTransient<ICompatibilityCheckerNuGetHandler, CompatibilityCheckerNuGetHandler>();
            serviceCollection.AddTransient<IPortingAssistantAnalysisHandler, PortingAssistantAnalysisHandler>();
            serviceCollection.AddTransient<ICompatibilityCheckerRecommendationHandler, CompatibilityCheckerRecommendationHandler>();
            serviceCollection.AddTransient<ICompatibilityCheckerHandler, CompatibilityCheckerHandler>();

            serviceCollection.AddTransient<IPortingHandler, PortingHandler>();
            serviceCollection.AddTransient<IPortingProjectFileHandler, PortingProjectFileHandler>();
            serviceCollection.AddHttpClient("s3")
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetRetryPolicy());
            serviceCollection.AddHttpClient("github")
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetRetryPolicy());
            serviceCollection.AddSingleton<ICacheManager, CacheManager>();
            serviceCollection.AddSingleton<ICacheService, CacheService>();

            serviceCollection.AddTransient<ICompatibilityChecker, Compatibility.Core.Checkers.ExternalCompatibilityChecker>();
            serviceCollection.AddTransient<ICompatibilityChecker, Compatibility.Core.Checkers.SdkCompatibilityChecker>();
            serviceCollection.AddTransient<ICompatibilityChecker, Compatibility.Core.Checkers.PortabilityAnalyzerCompatibilityChecker>();
            serviceCollection.AddTransient<IHttpService, Compatibility.Common.Utils.HttpService>();
            serviceCollection.AddTransient<IRegionalDatastoreService, Compatibility.Common.Utils.RegionalDatastoreService>();
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
