using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Codelyzer.Analysis;
using Codelyzer.Analysis.Model;
using NuGet.Versioning;
using PortingAssistant.Client.Model;
using TextSpan = PortingAssistant.Client.Model.TextSpan;

namespace PortingAssistant.Client.Analysis.Utils
{
    public static class InvocationExpressionModelToInvocations
    {
        public static List<SourceFileAnalysisResult> AnalyzeResults(
            Dictionary<string, List<CodeEntityDetails>> sourceFileToInvocations,
            Dictionary<PackageVersionPair, Task<PackageDetails>> packageResults,
            Dictionary<string, Task<RecommendationDetails>> recommendationResults,
            string tragetFramework = "netcoreapp3.1"
        )
        {
            var packageDetailsWithIndicesResults = ApiCompatiblity.PreProcessPackageDetails(packageResults);
            return sourceFileToInvocations.Select(sourceFile =>
            {
                return new SourceFileAnalysisResult
                {
                    SourceFileName = Path.GetFileName(sourceFile.Key),
                    SourceFilePath = sourceFile.Key,
                    ApiAnalysisResults = sourceFile.Value.Select(invocation =>
                    {
                        var package = invocation.Package;
                        var sdkpackage = new PackageVersionPair { PackageId = invocation.Namespace, Version = "0.0.0", PackageSourceType = PackageSourceType.SDK };

                        // check result with nuget package
                        var packageDetails = packageDetailsWithIndicesResults.GetValueOrDefault(package, null);
                        var compatibilityResultWithPackage = ApiCompatiblity.GetCompatibilityResult(packageDetails,
                                                 invocation.OriginalDefinition,
                                                 invocation.Package.Version);

                        // potential check with namespace
                        var sdkpackageDetails = packageDetailsWithIndicesResults.GetValueOrDefault(sdkpackage, null);
                        var compatibilityResultWithSdk = ApiCompatiblity.GetCompatibilityResult(sdkpackageDetails,
                                                 invocation.OriginalDefinition,
                                                 invocation.Package.Version);

                        var compatibilityResult = GetCompatibilityResult(compatibilityResultWithPackage, compatibilityResultWithSdk);

                        var recommendationDetails = recommendationResults.GetValueOrDefault(invocation.Namespace, null);
                        var apiRecommendation = ApiCompatiblity.UpgradeStrategy(
                                                compatibilityResult,
                                                invocation.OriginalDefinition,
                                                recommendationDetails);


                        return new ApiAnalysisResult
                        {
                            CodeEntityDetails = invocation,
                            CompatibilityResults = new Dictionary<string, CompatibilityResult>
                            {
                                { tragetFramework, compatibilityResult}
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

                        var assemblyLength = invocation.Reference?.Assembly?.Length;
                        if (assemblyLength == null || assemblyLength == 0)
                        {
                            return null;
                        }

                        // Check if invocation is from Nuget
                        var potentialNugetPackage = analyzer?.ProjectResult?.ExternalReferences?.NugetReferences?.Find((n) =>
                           n.AssemblyLocation?.EndsWith(invocation.Reference.Assembly + ".dll") == true);

                        if (potentialNugetPackage == null)
                        {
                            potentialNugetPackage = analyzer?.ProjectResult?.ExternalReferences?.NugetDependencies?.Find((n) =>
                           n.AssemblyLocation?.EndsWith(invocation.Reference.Assembly + ".dll") == true);
                        }
                        PackageVersionPair nugetPackage = ReferenceToPackageVersionPair(potentialNugetPackage);

                        // Check if invocation is from SDK
                        var potentialSdk = analyzer?.ProjectResult?.ExternalReferences?.SdkReferences?.Find((s) =>
                            s.AssemblyLocation?.EndsWith(invocation.Reference.Assembly + ".dll") == true);
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

        public static CompatibilityResult GetCompatibilityResult(CompatibilityResult compatibilityResultWithPackage, CompatibilityResult compatibilityResultWithSdk)
        {
            var compatiblityResult = compatibilityResultWithPackage;

            switch (compatibilityResultWithPackage.Compatibility)
            {
                case Compatibility.COMPATIBLE:
                    break;

                case Compatibility.INCOMPATIBLE:
                    if (compatibilityResultWithSdk.Compatibility == Compatibility.COMPATIBLE)
                    {
                        compatiblityResult = compatibilityResultWithSdk;
                    }
                    break;

                case Compatibility.DEPRECATED:
                    if (compatibilityResultWithSdk.Compatibility == Compatibility.COMPATIBLE ||
                        compatibilityResultWithSdk.Compatibility == Compatibility.INCOMPATIBLE)
                    {
                        compatiblityResult = compatibilityResultWithSdk;
                    }
                    break;

                case Compatibility.UNKNOWN:
                    if (compatibilityResultWithSdk.Compatibility == Compatibility.COMPATIBLE ||
                        compatibilityResultWithSdk.Compatibility == Compatibility.INCOMPATIBLE
                        || compatibilityResultWithSdk.Compatibility == Compatibility.DEPRECATED)
                    {
                        compatiblityResult = compatibilityResultWithSdk;
                    }
                    break;

                default:
                    break;
            }

            return compatiblityResult;
        }

        public static PackageVersionPair ReferenceToPackageVersionPair(ExternalReference reference, PackageSourceType sourceType = PackageSourceType.NUGET)
        {
            if (reference != null)
            {
                string version = reference.Version ?? "";
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
