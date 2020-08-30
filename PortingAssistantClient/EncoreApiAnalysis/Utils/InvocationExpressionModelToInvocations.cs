using System.Collections.Generic;
using System.Linq;
using AwsCodeAnalyzer.Model;
using EncoreCommon.Model;
using EncoreCache;
using NuGet.Versioning;
using System.Threading.Tasks;

namespace EncoreApiAnalysis.Utils
{
    public static class InvocationExpressionModelToInvocations
    {
        public static Dictionary<string, List<InvocationWithCompatibility>> Convert(
            Dictionary<string, List<InvocationExpression>> sourceFileToInvocations,
            Project project, IEncoreCacheHandler handler)
        {

            return sourceFileToInvocations.Select(sourceFile =>
                KeyValuePair.Create(
                    sourceFile.Key,
                    sourceFile.Value.Select(invocation =>
                    {
                        var potentialNugetPackages = project.NugetDependencies.FindAll((n) => invocation.SemanticNamespace.ToLower().Contains(n.PackageId.ToLower()));
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

                        return new InvocationWithCompatibility
                        {
                            invocation = new Invocation
                            {
                                MethodName = invocation.MethodName,
                                Namespace = invocation.SemanticNamespace,
                                MethodSignature = invocation.SemanticMethodSignature,
                                OriginalDefinition = invocation.SemanticOriginalDefinition,
                                Location = new InvocationLocation
                                {
                                    StartCharPosition = invocation.TextSpan.StartCharPosition,
                                    EndCharPosition = invocation.TextSpan.EndCharPosition,
                                    StartLinePosition = invocation.TextSpan.StartLinePosition,
                                    EndLinePosition = invocation.TextSpan.EndLinePosition
                                },
                                PackageId = nugetPackage?.PackageId,
                                Version = nugetVersion?.ToNormalizedString()
                            },
                            isCompatible = ApiCompatiblity.apiInPackageVersion(
                                packageDetails.Result,
                                invocation.SemanticOriginalDefinition,
                                nugetVersion?.ToNormalizedString()),
                            deprecated = packageDetails.Result.Deprecated,
                            replacement = ApiCompatiblity.upgradeStrategy(
                                packageDetails.Result,
                                invocation.SemanticOriginalDefinition,
                                nugetVersion?.ToNormalizedString())
                        };
                    })
                    .Where(invocation => invocation != null)
                    .ToList()
                )
            ).ToDictionary(p => p.Key, p => p.Value);

        }
    }
}
