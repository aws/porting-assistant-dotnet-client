using System;
using System.Collections.Generic;
using System.Linq;
using Codelyzer.Analysis.Model;
using NuGet.Versioning;
using PortingAssistant.Client.Model;
using TextSpan = PortingAssistant.Client.Model.TextSpan;

namespace PortingAssistant.Client.Analysis.Utils
{
    public static class CodeEntityModelToCodeEntities
    {
        

        public static Dictionary<string, List<CodeEntityDetails>> Convert(
             Dictionary<string, UstList<UstNode>> sourceFileToCodeEntities,
             AnalyzerResult analyzer)
        {
            return Convert(sourceFileToCodeEntities, analyzer?.ProjectResult?.ExternalReferences);
        }

        public static Dictionary<string, List<CodeEntityDetails>> Convert(
             Dictionary<string, UstList<UstNode>> sourceFileToCodeEntities,
             ExternalReferences externalReferences)
        {
            return sourceFileToCodeEntities.Select(sourceFile =>
            {
                return KeyValuePair.Create(sourceFile.Key, sourceFile.Value.Select(node =>
                {
                    if (node is InvocationExpression invocationExpression)
                    {
                        return Convert(invocationExpression, externalReferences);
                    }
                    else if (node is DeclarationNode declarationNode)
                    {
                        return Convert(declarationNode, externalReferences);
                    }
                    else if (node is Annotation annotation)
                    {
                        return Convert(annotation, externalReferences);
                    }
                    else if (node is StructDeclaration structDeclaration)
                    {
                        return Convert(structDeclaration, externalReferences);
                    }
                    else if (node is EnumDeclaration enumDeclaration)
                    {
                        return Convert(enumDeclaration, externalReferences);
                    }
                    else if (node is EnumBlock enumBlock)
                    {
                        return Convert(enumBlock, externalReferences);
                    }

                    return null;
                }).Where(result => result != null).ToList());
            }).ToDictionary(result => result.Key, result => result.Value);
        }

        public static CodeEntityDetails Convert(InvocationExpression node, ExternalReferences externalReferences)
        {
            return CreateCodeEntityDetails(node.MethodName, 
                node.SemanticNamespace, 
                string.IsNullOrEmpty(node.SemanticMethodSignature) ? node.MethodName : node.SemanticMethodSignature,
                string.IsNullOrEmpty(node.SemanticOriginalDefinition) ? node.MethodName : node.SemanticOriginalDefinition,
                CodeEntityType.Method, 
                node, 
                node.Reference, 
                externalReferences);
        }

        public static CodeEntityDetails Convert(DeclarationNode node, ExternalReferences externalReferences)
        {
            return CreateCodeEntityDetails(node.Identifier, node.Reference.Namespace, node.Identifier, node.Identifier, CodeEntityType.Declaration, node, node.Reference, externalReferences);
        }

        public static CodeEntityDetails Convert(Annotation node, ExternalReferences externalReferences)
        {
            return CreateCodeEntityDetails(node.Identifier, node.Reference.Namespace, node.Identifier, node.Identifier, CodeEntityType.Annotation, node, node.Reference, externalReferences);
        }

        public static CodeEntityDetails Convert(EnumDeclaration node, ExternalReferences externalReferences)
        {
            return CreateCodeEntityDetails(node.Identifier, node.Reference.Namespace, node.Identifier, node.Identifier, CodeEntityType.Enum, node, node.Reference, externalReferences);
        }

        public static CodeEntityDetails Convert(StructDeclaration node, ExternalReferences externalReferences)
        {
            return CreateCodeEntityDetails(node.Identifier, node.Reference.Namespace, node.Identifier, node.Identifier, CodeEntityType.Struct, node, node.Reference, externalReferences);
        }

        public static CodeEntityDetails Convert(EnumBlock node, ExternalReferences externalReferences)
        {
            return CreateCodeEntityDetails(node.Identifier, node.Reference.Namespace, node.Identifier, node.Identifier, CodeEntityType.Enum, node, node.Reference, externalReferences);
        }

        public static CodeEntityDetails Convert(AttributeList node, ExternalReferences externalReferences)
        {
            return CreateCodeEntityDetails(node.Identifier, node.Reference.Namespace, node.Identifier, node.Identifier, CodeEntityType.Annotation, node, node.Reference, externalReferences);
        }

        private static CodeEntityDetails CreateCodeEntityDetails(
            string name,
            string @namespace,
            string signature,
            string originalDefinition,
            CodeEntityType codeEntityType,
            UstNode ustNode,
            Reference reference,
            ExternalReferences externalReferences)
        {
            var package = GetPackageVersionPair(reference, externalReferences, @namespace);

            if (package == null)
            {
                //If any of these values are populated, this is an internal reference. If they are all null, this is a code entity with no references
                if (reference?.Assembly != null
                    || reference?.Namespace != null
                    || reference?.AssemblySymbol != null
                    || reference?.Version != null
                    || reference?.AssemblyLocation != null
                    || !string.IsNullOrEmpty(@namespace))
                {
                    return null;
                }
            }

            // Otherwise return the code entity
            return CreateCodeEntity(name, @namespace, signature, package, originalDefinition,
                codeEntityType, ustNode);
        }


        private static CodeEntityDetails CreateCodeEntity(string name,
            string @namespace,
            string signature,
            PackageVersionPair package,
            string originalDefinition,
            CodeEntityType codeEntityType,
            UstNode ustNode)
        {
            return new CodeEntityDetails
            {
                Name = name,
                Namespace = @namespace ?? string.Empty,
                Signature = signature,
                OriginalDefinition = originalDefinition,
                CodeEntityType = codeEntityType,
                TextSpan = new TextSpan
                {
                    StartCharPosition = ustNode.TextSpan?.StartCharPosition,
                    EndCharPosition = ustNode.TextSpan?.EndCharPosition,
                    StartLinePosition = ustNode.TextSpan?.StartLinePosition,
                    EndLinePosition = ustNode.TextSpan?.EndLinePosition
                },
                // If we found an matching sdk assembly, assume the code is using the sdk.
                Package = package ?? new PackageVersionPair() { PackageId = "", Version = "" }
            };
        }

        private static PackageVersionPair GetPackageVersionPair(Reference reference, ExternalReferences externalReferences, string @namespace)
        {
            var assemblyLength = reference?.Assembly?.Length;
            if (assemblyLength == null || assemblyLength == 0)
            {
                return null;
            }

            // Check if code entity is from Nuget
            var potentialNugetPackage = externalReferences?.NugetReferences?.Find((n) =>
               n.AssemblyLocation?.EndsWith(reference.Assembly + ".dll") == true || n.Identity.Equals(reference.Assembly));

            if (potentialNugetPackage == null)
            {
                potentialNugetPackage = externalReferences?.NugetDependencies?.Find((n) =>
               n.AssemblyLocation?.EndsWith(reference.Assembly + ".dll") == true || n.Identity.Equals(reference.Assembly));
            }
            PackageVersionPair nugetPackage = ReferenceToPackageVersionPair(potentialNugetPackage);

            // Check if code entity is from SDK
            var potentialSdk = externalReferences?.SdkReferences?.Find((s) =>
                s.AssemblyLocation?.EndsWith(reference.Assembly + ".dll") == true || s.Identity.Equals(reference.Assembly));
            PackageVersionPair sdk = ReferenceToPackageVersionPair(potentialSdk, PackageSourceType.SDK);

            // if mscorlib, try to match namespace to sdk
            if (string.Compare(reference.Assembly, "mscorlib", StringComparison.OrdinalIgnoreCase) == 0)
            {
                var potential = externalReferences?.SdkReferences?.Find(s =>
                    s.AssemblyLocation?.EndsWith($"{@namespace}.dll") == true ||
                    s.Identity.Equals(@namespace));
                if (potential != null)
                {
                    potential.Version = "0.0.0"; // SDK lookups will have no version number
                    sdk = ReferenceToPackageVersionPair(potential, PackageSourceType.SDK);
                }
            }

            return sdk ?? nugetPackage;
        }
        /*
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
                        compatibilityResultWithSdk.Compatibility == Compatibility.INCOMPATIBLE || 
                        compatibilityResultWithSdk.Compatibility == Compatibility.DEPRECATED)
                    {
                        compatiblityResult = compatibilityResultWithSdk;
                    }
                    break;

                default:
                    break;
            }

            return compatiblityResult;
        }
        */
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
