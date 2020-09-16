using System.Collections.Generic;
using System.Linq;
using AwsCodeAnalyzer.Model;
using PortingAssistant.Model;
using PortingAssistant.NuGet;
using NuGet.Versioning;
using System.Threading.Tasks;
using PortingAssistant.ApiAnalysis.Utils;
using TextSpan = PortingAssistant.Model.TextSpan;
using System.IO;

namespace PortingAssistantApiAnalysis.Utils
{
    public static class InvocationExpressionModelToInvocations
    {
        public static List<SourceFileAnalysisResult> Convert(
            Dictionary<string, List<InvocationExpression>> sourceFileToInvocations,
            ProjectDetails project, IPortingAssistantNuGetHandler handler,
            Dictionary<PackageVersionPair, Task<PackageDetails>> namespaceresults,
            Dictionary<string, Task<RecommendationDetails>> recommendationDetails
        )
        {

            return sourceFileToInvocations.Select(sourceFile =>
            {
                return new SourceFileAnalysisResult
                {
                    SourceFileName = Path.GetFileName(sourceFile.Key),
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

                        Task<PackageDetails> packageDetails = null;
                        if (nugetPackage != null)
                        {
                            NuGetVersion.TryParse(nugetPackage.Version, out nugetVersion);
                            packageDetails = handler.GetNugetPackages(new List<PackageVersionPair> { nugetPackage}, "")
                                            .GetValueOrDefault(nugetPackage);
                        }
                        else
                        {
                            packageDetails = namespaceresults.GetValueOrDefault(new PackageVersionPair
                            {
                                PackageId = invocation.SemanticNamespace,
                                Version = "0.0.0"
                            });
                        }

                        var compatibilityResult = ApiCompatiblity.GetCompatibilityResult(packageDetails,
                                                 invocation.SemanticOriginalDefinition,
                                                 nugetVersion?.ToNormalizedString());

                        var apiRecommendation = ApiCompatiblity.UpgradeStrategy(
                                                packageDetails,
                                                invocation.SemanticOriginalDefinition,
                                                nugetVersion?.ToNormalizedString(),
                                                invocation.SemanticNamespace,
                                                recommendationDetails);


                        return new ApiAnalysisResult {
                            CodeEntityDetails = new CodeEntityDetails
                            {
                                Name = invocation.MethodName,
                                Namespace = invocation.SemanticNamespace,
                                Signature = invocation.SemanticMethodSignature,
                                OriginalDefinition = invocation.SemanticOriginalDefinition,
                                TextSpan = new TextSpan
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
                            CompatibilityResults = new Dictionary<string, CompatibilityResult>
                            {
                                { ApiCompatiblity.DEFAULT_TARGET, compatibilityResult}
                                    
                            },
                            Recommendations = new Recommendations
                            {
                                RecommendedActions = new List<RecommendedAction>
                                {
                                    apiRecommendation
                                }
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
