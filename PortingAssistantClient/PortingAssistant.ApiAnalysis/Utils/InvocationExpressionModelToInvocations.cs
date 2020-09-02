﻿using System.Collections.Generic;
using System.Linq;
using AwsCodeAnalyzer.Model;
using PortingAssistant.Model;
using PortingAssistant.NuGet;
using NuGet.Versioning;
using System.Threading.Tasks;
using PortingAssistant.ApiAnalysis.Utils;
using TextSpan = PortingAssistant.Model.TextSpan;

namespace PortingAssistantApiAnalysis.Utils
{
    public static class InvocationExpressionModelToInvocations
    {
        public static List<SourceFileAnalysisResult> Convert(
            Dictionary<string, List<InvocationExpression>> sourceFileToInvocations,
            ProjectDetails project, IPortingAssistantNuGetHandler handler)
        {

            return sourceFileToInvocations.Select(sourceFile =>
            {
                return new SourceFileAnalysisResult
                {
                    SourceFilePath = sourceFile.Key,
                    ApiAnalysisResults = sourceFile.Value.Select(invocation =>
                    {
                        var potentialNugetPackages = project.PackageReferences.FindAll((n) => invocation.SemanticNamespace.ToLower().Contains(n.PackageId.ToLower()));
                        PackageVersionPair nugetPackage = null;
                        if (potentialNugetPackages.Count() > 0)
                        {
                            nugetPackage = potentialNugetPackages.Aggregate((max, cur) => cur.PackageId.Length > max.PackageId.Length ? cur : max);
                        }
                        NuGetVersion nugetVersion = null;
                        if (nugetPackage != null)
                        {
                            NuGetVersion.TryParse(nugetPackage.Version, out nugetVersion);
                        }
                        var packageDetails = handler.GetPackageDetails(nugetPackage);
                        packageDetails.Wait();
                        return new ApiAnalysisResult {
                            Invocation = new Invocation
                            {
                                MethodName = invocation.MethodName,
                                Namespace = invocation.SemanticNamespace,
                                MethodSignature = invocation.SemanticMethodSignature,
                                OriginalDefinition = invocation.SemanticOriginalDefinition,
                                Location = new TextSpan
                                {
                                    StartCharPosition = invocation.TextSpan.StartCharPosition,
                                    EndCharPosition = invocation.TextSpan.EndCharPosition,
                                    StartLinePosition = invocation.TextSpan.StartLinePosition,
                                    EndLinePosition = invocation.TextSpan.EndLinePosition
                                },
                                Package = new PackageVersionPair
                                {
                                    PackageId = nugetPackage?.PackageId,
                                    Version = nugetVersion?.ToNormalizedString()
                                }
                            },
                            CompatibilityResult = ApiCompatiblity.apiInPackageVersion(
                                packageDetails.Result,
                                invocation.SemanticOriginalDefinition,
                                nugetVersion?.ToNormalizedString()) ? Compatibility.COMPATIBLE : Compatibility.INCOMPATIBLE,
                            isDeprecated = packageDetails.Result.Deprecated,
                            ApiRecommendation = new ApiRecommendation
                            {
                                RecommendedActionType = RecommendedActionType.UpgradePackage,
                                UpgradeVersion = ApiCompatiblity.upgradeStrategy(
                                                packageDetails.Result,
                                                invocation.SemanticOriginalDefinition,
                                                nugetVersion?.ToNormalizedString())
                                 
                                
                            }
                        };
                    }).Where(invocation => invocation != null)
                    .ToList()
                };
            }
            ).ToList();
        }
    }
}
