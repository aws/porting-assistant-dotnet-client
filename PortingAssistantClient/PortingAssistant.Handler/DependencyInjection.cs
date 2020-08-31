﻿using Amazon;
using Amazon.S3.Transfer;
using PortingAssistant.ApiAnalysis;
using PortingAssistant.NuGet;
using PortingAssistant.Model;
using PortingAssistant.InternalNuGetChecker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace PortingAssistantHandler
{
    public static class DependencyInjection
    {
        public static void AddAssessment(this IServiceCollection serviceCollection, IConfiguration cacheConfig)
        {
            serviceCollection.AddSingleton<IAssessmentHandler, AssessmentHandler>();
            serviceCollection.AddSingleton<IPortingAssistantInternalNuGetCompatibilityHandler, PortingAssistantInternalNuGetCompatibilityHandler>();
            serviceCollection.Configure<EndpointOptions>(cacheConfig);
            serviceCollection.AddSingleton<IPortingAssistantNuGetHandler, PortingAssistantNuGetHandler>();
            serviceCollection.AddSingleton<IPortingAssistantApiAnalysisHandler, PortingAssistantApiAnalysisHandler>();
            serviceCollection.AddSingleton<ICompatibilityChecker, InternalPackagesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, ExternalPackagesCompatibilityChecker>();
            serviceCollection.AddSingleton<ICompatibilityChecker, PortabilityAnalyzerCompatibilityChecker>();
            serviceCollection.AddSingleton<ITransferUtility, TransferUtility>();
        }
    }
}
