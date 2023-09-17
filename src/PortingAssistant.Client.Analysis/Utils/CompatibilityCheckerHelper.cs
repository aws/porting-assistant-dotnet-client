using Codelyzer.Analysis;
using Codelyzer.Analysis.Model;
using PortingAssistant.Client.Analysis.Mappers;
using PortingAssistant.Client.Model;
using PortingAssistant.Compatibility.Common.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompatibilityResult = PortingAssistant.Client.Model.CompatibilityResult;

namespace PortingAssistant.Client.Analysis.Utils
{
    public class CompatibilityCheckerHelper
    {

        public static List<SourceFileAnalysisResult> AddCompatibilityCheckerResultsToCodeEntities
            (
            Dictionary<string, List<CodeEntityDetails>> sourceFileToCodeEntities,
            CompatibilityCheckerResponse compatibilityCheckerResponse,
            Dictionary<string, List<RecommendedAction>> portingActionResults,
            string targetFramework = "net6.0",
            bool compatibleOnly = false
        )
        {
            return sourceFileToCodeEntities.Select(sourceFile =>
            {
                return new SourceFileAnalysisResult
                {
                    SourceFileName = Path.GetFileName(sourceFile.Key),
                    SourceFilePath = sourceFile.Key,
                    RecommendedActions = portingActionResults?.GetValueOrDefault(sourceFile.Key, new List<RecommendedAction>()),
                    ApiAnalysisResults = sourceFile.Value.Select(codeEntity =>
                    {
                        var package = PackageVersionPairMapper.Convert(codeEntity.Package);
                        //A code entity with no reference data. This can be any error in the code
                        if (package == null)
                        {
                            return new ApiAnalysisResult
                            {
                                CodeEntityDetails = codeEntity,
                                CompatibilityResults = new Dictionary<string, CompatibilityResult>
                                {
                                    {
                                        targetFramework, new CompatibilityResult() { Compatibility = Model.Compatibility.UNKNOWN, CompatibleVersions = new List<string>() }
                                    }
                                },
                                Recommendations = new Model.Recommendations
                                {
                                    RecommendedActions = new List<RecommendedAction>() {

                                        new RecommendedAction() {
                                            RecommendedActionType = Model.RecommendedActionType.NoRecommendation,
                                            Description = null,
                                            TextSpan = null,
                                            TargetCPU = null,
                                            TextChanges = null
                                        }
                                    },
                                    RecommendedPackageVersions = new List<string>()
                                }
                            };
                        }

                        ApiAnalysisResult apiAnalysisResult = RetrieveApiAnalysisResultFromCompatibilityCheckResponse(compatibilityCheckerResponse, targetFramework, codeEntity, package);

                        if (apiAnalysisResult == null)
                        {
                            var sdkPackage = new Compatibility.Common.Model.PackageVersionPair
                            {
                                PackageId = codeEntity.Namespace, Version = "0.0.0",
                                PackageSourceType = Compatibility.Common.Model.PackageSourceType.SDK
                            };

                            apiAnalysisResult = RetrieveApiAnalysisResultFromCompatibilityCheckResponse(compatibilityCheckerResponse, targetFramework, codeEntity, sdkPackage);

                        }

                        if (apiAnalysisResult == null)
                        {
                            return new ApiAnalysisResult
                            {
                                CodeEntityDetails = codeEntity,
                                CompatibilityResults = new Dictionary<string, CompatibilityResult>
                                {
                                    {
                                        targetFramework, new CompatibilityResult() { Compatibility = Model.Compatibility.UNKNOWN, CompatibleVersions = new List<string>() }
                                    }
                                },
                                Recommendations = new Model.Recommendations
                                {
                                    RecommendedActions = new List<RecommendedAction>() {
                                        new RecommendedAction()
                                        {
                                            RecommendedActionType = Model.RecommendedActionType.NoRecommendation,
                                            Description = null,
                                            TextSpan = null,
                                            TargetCPU = null,
                                            TextChanges = null
                                        },
                                    },
                                    RecommendedPackageVersions = new List<string>()
                                }
                            };
                        }

                        if (compatibleOnly)
                        {
                            if (!(apiAnalysisResult.CompatibilityResults[targetFramework].Compatibility == Model.Compatibility.INCOMPATIBLE))
                                return null;
                        }
                        return apiAnalysisResult;


                    }).Where(codeEntity => codeEntity != null)
                    .ToList()
                };
            }
            ).ToList();
        }

        private static ApiAnalysisResult RetrieveApiAnalysisResultFromCompatibilityCheckResponse(
            CompatibilityCheckerResponse compatibilityCheckerResponse,
            string targetFramework,
            CodeEntityDetails codeEntity,
            Compatibility.Common.Model.PackageVersionPair package)
        {
            ApiAnalysisResult apiAnalysisResult = null;

            if (compatibilityCheckerResponse == null)
            {
                //Default value will be assigned to ApiAnalysisResult
                return null;
            }
                 
            //assume the package is nuget
            if (compatibilityCheckerResponse.ApiAnalysisResults!= null && compatibilityCheckerResponse.ApiAnalysisResults.ContainsKey(package))
            {
                if (compatibilityCheckerResponse.ApiAnalysisResults[package].ContainsKey(codeEntity.OriginalDefinition))
                {
                    var compatibilityResultFromResponse = compatibilityCheckerResponse.ApiAnalysisResults[package][codeEntity.OriginalDefinition].CompatibilityResults;
                    var recommandationFromResponse = compatibilityCheckerResponse.ApiAnalysisResults[package][codeEntity.OriginalDefinition].Recommendations;
                    if (recommandationFromResponse == null &&
                        compatibilityCheckerResponse.ApiRecommendationResults != null &&
                        compatibilityCheckerResponse.ApiRecommendationResults.ContainsKey(package) &&
                        compatibilityCheckerResponse.ApiRecommendationResults[package].ContainsKey(codeEntity.OriginalDefinition)
                        )
                    {
                        recommandationFromResponse = compatibilityCheckerResponse.ApiRecommendationResults[package][codeEntity.OriginalDefinition].Recommendations;
                    }

                    apiAnalysisResult = new ApiAnalysisResult
                    {
                        CodeEntityDetails = codeEntity,


                        CompatibilityResults = new Dictionary<string, CompatibilityResult>
                        {
                            { targetFramework,
                               CompatibilityResultMapper.Convert(compatibilityResultFromResponse?.GetValueOrDefault(targetFramework, null))
                            }
                        },
                        Recommendations = new Model.Recommendations
                        {
                            RecommendedActions = RecommandationMapper.Convert(recommandationFromResponse?.RecommendedActions),
                            RecommendedPackageVersions = recommandationFromResponse?.RecommendedPackageVersions
                        }
                    };
                }
            }

            return apiAnalysisResult;
        }

        private static KeyValuePair<string, UstList<UstNode>> SourceFileToCodeTokens(RootUstNode sourceFile)
        {
            var allNodes = new UstList<UstNode>();
            allNodes.AddRange(sourceFile.AllInvocationExpressions());
            allNodes.AddRange(sourceFile.AllAnnotations());
            allNodes.AddRange(sourceFile.AllDeclarationNodes());
            allNodes.AddRange(sourceFile.AllStructDeclarations());
            allNodes.AddRange(sourceFile.AllEnumDeclarations());
            allNodes.AddRange(sourceFile.AllEnumBlocks());
            allNodes.AddRange(sourceFile.AllAttributeLists());

            return KeyValuePair.Create(sourceFile.FileFullPath, allNodes);
        }
        public static CompatibilityCheckerRequest ConvertAnalyzeResultToCompatibilityCheckerRequest(
            string solutionFileName, AnalyzerResult analyzer,
            string targetFramework, out Dictionary<string,
            List<CodeEntityDetails>> sourceFileToCodeEntityDetails,
            AssessmentType assessmentType = AssessmentType.FullAssessment)
        {
            sourceFileToCodeEntityDetails = null;
            if (analyzer == null || analyzer.ProjectResult == null)
            {
                return null;
            }

            var sourceFileToCodeTokens = analyzer.ProjectResult.SourceFileResults.Select((sourceFile) =>
            {
                return SourceFileToCodeTokens(sourceFile);
            }).ToDictionary(p => p.Key, p => p.Value);

            sourceFileToCodeEntityDetails = CodeEntityModelToCodeEntities.Convert(sourceFileToCodeTokens, analyzer);

            var namespaces = sourceFileToCodeEntityDetails.Aggregate(new HashSet<string>(), (agg, cur) =>
            {
                agg.UnionWith(cur.Value.Select(i => i.Namespace).Where(i => i != null));
                return agg;
            });

            var targetframeworks = analyzer.ProjectResult.TargetFrameworks.Count == 0 ?
                new List<string> { analyzer.ProjectResult.TargetFramework } : analyzer.ProjectResult.TargetFrameworks;

            var nugetPackages = analyzer.ProjectResult.ExternalReferences.NugetReferences
                .Select(r => CodeEntityModelToCodeEntities.ReferenceToPackageVersionPair(r))
                .ToHashSet();
            var nugetPackageNameLookup = nugetPackages.Select(package => package.PackageId).ToHashSet();

            var subDependencies = analyzer.ProjectResult.ExternalReferences.NugetDependencies
                .Select(r => CodeEntityModelToCodeEntities.ReferenceToPackageVersionPair(r))
                .ToHashSet();

            var sdkPackages = namespaces.Select(n => new Model.PackageVersionPair
            {
                PackageId = n,
                Version = "0.0.0",
                PackageSourceType = Model.PackageSourceType.SDK
            })
                .Where(pair =>
                    !string.IsNullOrEmpty(pair.PackageId) &&
                    !nugetPackageNameLookup.Contains(pair.PackageId));

            var allPackages = nugetPackages
                .Union(subDependencies)
                .Union(sdkPackages)
                .ToList();

            Dictionary<Compatibility.Common.Model.PackageVersionPair, HashSet<ApiEntity>> packageWithApis
                = new Dictionary<Compatibility.Common.Model.PackageVersionPair, HashSet<ApiEntity>>();

            var entireCodeEntityList = sourceFileToCodeEntityDetails.SelectMany(x => x.Value).ToList();
            foreach (var entity in entireCodeEntityList)
            {
                if (string.IsNullOrEmpty(entity.Namespace))
                {
                    //Console.WriteLine($"found empty Namespace for entity {entity.Name}");
                    continue; 
                }
                PortingAssistant.Compatibility.Common.Model.PackageVersionPair package;
                if (entity.Package.PackageSourceType == Model.PackageSourceType.SDK)
                {
                    package = new PortingAssistant.Compatibility.Common.Model.PackageVersionPair
                    {
                        PackageId = entity.Namespace,
                        Version = "0.0.0",
                        PackageSourceType = PortingAssistant.Compatibility.Common.Model.PackageSourceType.SDK
                    };
                }
                else
                {
                    package = PackageVersionPairMapper.Convert(entity.Package);
                }
                PortingAssistant.Compatibility.Common.Model.CodeEntityType codeEntityType;
                Enum.TryParse(entity.CodeEntityType.ToString(), out codeEntityType);
                if (!packageWithApis.ContainsKey(package))
                {
                    packageWithApis.Add(package, new HashSet<ApiEntity>()
                        {
                            new ApiEntity
                            {
                                Namespace = entity.Namespace, 
                                CodeEntityType = codeEntityType,
                                OriginalDefinition = entity.OriginalDefinition
                            }
                        });
                }
                else
                {
                    var existList = packageWithApis[package];
                    var exist = existList.Add(new ApiEntity
                    {
                        Namespace = entity.Namespace,
                        CodeEntityType = codeEntityType,
                        OriginalDefinition = entity.OriginalDefinition
                    });
                    packageWithApis[package] = existList;
                }
            }

            //add missing packages if they are not used in the codeEntity
            foreach (var p in allPackages)
            {
                var package = PackageVersionPairMapper.Convert(p);
                if (!packageWithApis.ContainsKey(package))
                {
                    packageWithApis.Add(package, new HashSet<ApiEntity>());
                }
            }

            var fileLanguage = analyzer.ProjectResult.SourceFileResults.FirstOrDefault().Language;
            Language language = fileLanguage == "Visual Basic"? Language.Vb: Language.CSharp;
            
            return new CompatibilityCheckerRequest() {
                Language = language,
                SolutionGUID = solutionFileName,
                PackageWithApis = packageWithApis,
                TargetFramework = targetFramework ,
                AssessmentType = assessmentType };
                
        }
    }
}
