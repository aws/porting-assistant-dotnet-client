using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using PortingAssistant.Compatibility.Common.Utils;
using PortingAssistant.Compatibility.Core.Checkers;

namespace PortingAssistant.Compatibility.Core
{
    public class CompatibilityCheckerBuilder
    {
        private ServiceCollection ServiceCollection;
        private readonly CompatibilityCheckerConfiguration Configuration;
        private readonly ICompatibilityCheckerNuGetHandler NuGetHandler;
        private readonly ICompatibilityCheckerRecommendationHandler RecommendationHandler;
        //private readonly ICompatibilityCheckerRecommendationActionHandler RecommendationActionHandler;
        private readonly IHttpService HttpService;
        private readonly ICompatibilityCheckerHandler CompatibilityCheckerHandler;

        private CompatibilityCheckerBuilder(CompatibilityCheckerConfiguration configuration)
        {
            this.Configuration = configuration;
            ConfigureServices();
            var services = ServiceCollection.BuildServiceProvider();
            this.NuGetHandler = services.GetService<ICompatibilityCheckerNuGetHandler>();
            this.RecommendationHandler = services.GetService<ICompatibilityCheckerRecommendationHandler>();
            //this.RecommendationActionHandler = services.GetService<ICompatibilityCheckerRecommendationActionHandler>();
            this.HttpService = services.GetService<IHttpService>();
            CompatibilityCheckerHandler = services.GetService<ICompatibilityCheckerHandler>();
        }

        public static CompatibilityCheckerBuilder Build(CompatibilityCheckerConfiguration configuration)
        {
            return new CompatibilityCheckerBuilder(configuration);
        }

        public IHttpService GetHttpService()
        {
            return HttpService;
        }

        public ICompatibilityCheckerHandler GetCompatibilityCheckerHandler()
        {
            return CompatibilityCheckerHandler;
        }

        public ICompatibilityCheckerNuGetHandler GetNuGetHandler()
        {
            return NuGetHandler;
        }

        public ICompatibilityCheckerRecommendationHandler GetRecommendationHandler()
        {
            return RecommendationHandler;
        }
        /*
        public ICompatibilityCheckerRecommendationActionHandler GetRecommendationActionHandler()
        {
            return RecommendationActionHandler;
        }*/

        private void ConfigureServices()
        {
            ServiceCollection = new ServiceCollection();
            ServiceCollection.AddTransient<ICompatibilityCheckerNuGetHandler, CompatibilityCheckerNuGetHandler>();
            ServiceCollection.AddTransient<ICompatibilityCheckerRecommendationHandler, CompatibilityCheckerRecommendationHandler>();
            //ServiceCollection.AddTransient<ICompatibilityCheckerRecommendationActionHandler, CompatibilityCheckerRecommendationActionHandler>();
            ServiceCollection.AddTransient<ICompatibilityChecker, NugetCompatibilityChecker>();
            ServiceCollection.AddTransient<ICompatibilityChecker, SdkCompatibilityChecker>();
            ServiceCollection.AddTransient<ICompatibilityChecker, PortabilityAnalyzerCompatibilityChecker>();
            ServiceCollection.AddTransient<ICompatibilityCheckerHandler, CompatibilityCheckerHandler>();
            ServiceCollection.AddTransient<IHttpService, HttpService>();
            ServiceCollection.AddHttpClient("s3")
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetRetryPolicy());
            ServiceCollection.AddHttpClient("github")
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetRetryPolicy());
            ServiceCollection.AddOptions();
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
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