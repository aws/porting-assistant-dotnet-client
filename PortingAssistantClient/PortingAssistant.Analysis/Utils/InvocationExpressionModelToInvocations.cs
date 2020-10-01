using System.Collections.Generic;
using System.Linq;
using AwsCodeAnalyzer.Model;
using PortingAssistant.Model;
using NuGet.Versioning;
using System.Threading.Tasks;
using TextSpan = PortingAssistant.Model.TextSpan;
using System.IO;
using AwsCodeAnalyzer;

namespace PortingAssistant.Analysis.Utils
{
    public static class InvocationExpressionModelToInvocations
    {
        public static List<SourceFileAnalysisResult> AnalyzeResults(
            Dictionary<string, List<CodeEntityDetails>> sourceFileToInvocations,
            Dictionary<PackageVersionPair, Task<PackageDetails>> packageResults,
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
                        var packageDetails = packageResults.GetValueOrDefault(invocation.Package, null);

                        var compatibilityResult = ApiCompatiblity.GetCompatibilityResult(packageDetails,
                                                 invocation.OriginalDefinition,
                                                 invocation.Package.Version);

                        var apiRecommendation = ApiCompatiblity.UpgradeStrategy(
                                                packageDetails,
                                                invocation.OriginalDefinition,
                                                invocation.Package.Version,
                                                invocation.Namespace,
                                                recommendationDetails);


                        return new ApiAnalysisResult
                        {
                            CodeEntityDetails = invocation,
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

        public static Dictionary<string, List<CodeEntityDetails>> Convert(
             Dictionary<string, UstList<InvocationExpression>> sourceFileToInvocations,
             AnalyzerResult analyzer)
        {

            return sourceFileToInvocations.Select(sourceFile =>
                KeyValuePair.Create(
                    sourceFile.Key,
                    sourceFile.Value.Select(invocation =>
                    {
                        if (invocation == null)
                        {
                            return null;
                        }

                        var assemblyLength = invocation.Reference?.Assembly?.Length;
                        if (assemblyLength == null || assemblyLength == 0)
                        {
                            return null;
                        }

                        // Check if invocation is from Nuget
                        var potentialNugetPackage = analyzer?.ProjectResult?.ExternalReferences?.NugetReferences?.Find((n) =>
                           n.AssemblyLocation != null && n.AssemblyLocation.EndsWith(invocation.Reference.Assembly + ".dll"));
                        if (potentialNugetPackage == null)
                        {
                            potentialNugetPackage = analyzer?.ProjectResult?.ExternalReferences?.NugetDependencies?.Find((n) =>
                           n.AssemblyLocation != null && n.AssemblyLocation.EndsWith(invocation.Reference.Assembly + ".dll"));
                        }
                        PackageVersionPair nugetPackage = ReferenceToPackageVersionPair(potentialNugetPackage);
                        // Check if invocation is from SDK
                        var potentialSdk = analyzer?.ProjectResult?.ExternalReferences?.SdkReferences?.Find((s) =>
                            s.AssemblyLocation != null && s.AssemblyLocation.EndsWith(invocation.Reference.Assembly + ".dll"));
                        PackageVersionPair sdk = ReferenceToPackageVersionPair(potentialSdk, PackageSourceType.SDK);

                        // If both nuget package and sdk are null, this invocation is from an internal project. Skip it.
                        if (nugetPackage == null && sdk == null)
                        {
                            return null;
                        }

                        // Otherwise return the invocation
                        return new CodeEntityDetails
                        {
                            Name = invocation.MethodName,
                            Namespace = invocation.SemanticNamespace,
                            Signature = invocation.SemanticMethodSignature,
                            OriginalDefinition = invocation.SemanticOriginalDefinition,
                            TextSpan = new TextSpan
                            {
                                StartCharPosition = invocation.TextSpan?.StartCharPosition,
                                EndCharPosition = invocation.TextSpan?.EndCharPosition,
                                StartLinePosition = invocation.TextSpan?.StartLinePosition,
                                EndLinePosition = invocation.TextSpan?.EndLinePosition
                            },
                            // If we found an matching sdk assembly, assume the code is using the sdk.
                            Package = sdk ?? nugetPackage,
                        };
                    })
                    .Where(invocation => invocation != null)
                    .ToList()
                )
            )
            .Where(p => p.Value.Count != 0)
            .ToDictionary(p => p.Key, p => p.Value);

        }

        public static PackageVersionPair ReferenceToPackageVersionPair(ExternalReference reference, PackageSourceType sourceType = PackageSourceType.NUGET)
        {
            if (reference != null)
            {
                string version = "0.0.0.0"; // If no package verison, mark as 0.0.0.0
                if (NuGetVersion.TryParse(reference.Version, out var parsedVersion))
                {
                    version = parsedVersion.ToNormalizedString();
                }
                return new PackageVersionPair { PackageId = reference.Identity, Version = version, PackageSourceType = sourceType };
            }
            return null;
        }
    }
}
