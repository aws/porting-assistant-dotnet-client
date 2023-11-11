using System;
using Microsoft.Extensions.Logging;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;
using System.Collections.Generic;
using System.IO;
using PortingAssistant.Client.Model;

namespace PortingAssistant.Client.Analysis
{
    public class CacheService : ICacheService
    {
        private readonly ILogger<CacheService> _logger;
        private readonly ICacheManager _cacheManager;
        public string CacheFilePath { get; set; }


        public CacheService(ICacheManager cacheManager, ILogger<CacheService> logger)
        {
            _cacheManager = cacheManager;
            _logger = logger;
        }

        public bool IsCacheAvailable()
        {
            //if cache file doesn't exist
            if (string.IsNullOrEmpty(CacheFilePath) || !File.Exists(CacheFilePath))
            {
                return false;
            }
            return _cacheManager.DoseCacheObjectsContainData();
        }

        public void ValidateCacheFile(PortingAssistantConfiguration config)
        {
            ValidateCacheFile(config.CompatibilityCheckerCacheFilePath, config.CacheExpirationHours);
        }

        public void ValidateCacheFile(string cacheFilePath, int cacheExpirationHours = 24)
        {
            try
            {
                if (!string.IsNullOrEmpty(cacheFilePath))
                {
                    CacheFilePath = cacheFilePath;
                }
                //CacheFilePath = config.CompatibilityCheckerCacheFilePath;
                var expirationHours = cacheExpirationHours;
                if (!string.IsNullOrEmpty(CacheFilePath) && File.Exists(CacheFilePath))
                {
                    _logger.LogInformation($"Cache file found: [{CacheFilePath}]");
                    var fileInfo = new FileInfo(CacheFilePath);
                    //cache file was created more than 24 hours ago , need to delete it
                    if ((DateTime.Now - fileInfo.CreationTime).TotalHours > expirationHours)
                    {
                        fileInfo.Delete();
                        _cacheManager.Clear();
                    }
                    else
                    {
                        Exception ex = null;
                        _cacheManager.TryLoadCacheObjectFromLocalFile(File.ReadAllText(CacheFilePath), out ex);
                        if (ex != null)
                        {
                            //if fail to load cache from file, delete the file. 
                            fileInfo.Delete();
                            _logger.LogError(ex, "fail to call _cacheManager.TryLoadCacheObjectFromLocalFile. No Cache loaded ");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation($"No cache file found: [{CacheFilePath}]");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to ValidateCacheFile");

            }
        }

        public void ApplyCacheToCompatibleCheckerResults(CompatibilityCheckerRequest request,
            List<Compatibility.Common.Model.PackageVersionPair> allPackages,
            out Dictionary<Compatibility.Common.Model.PackageVersionPair, HashSet<ApiEntity>> packageWithApisNeedToCheck,
            ref Dictionary<Compatibility.Common.Model.PackageVersionPair, AnalysisResult> packageAnalysisResultsDic,
            ref Dictionary<Compatibility.Common.Model.PackageVersionPair, Dictionary<string, AnalysisResult>> apiAnalysisResultsDic)
        {
            var targetFramework = request.TargetFramework;
            var packagesOnlyNeedToCheck = new HashSet<Compatibility.Common.Model.PackageVersionPair>();
            packageWithApisNeedToCheck = new Dictionary<Compatibility.Common.Model.PackageVersionPair, HashSet<ApiEntity>>();

            // Load results from cache object
            foreach (var package in allPackages)
            {
                try
                {
                    // TODO: HasCachedPackageAnalysisResult
                    if (_cacheManager.CacheExists(package, targetFramework))
                    {
                        packageAnalysisResultsDic.Add(package, _cacheManager.Get(package, targetFramework));
                        _logger.LogInformation($"{package} exist in package cache");
                    }
                    else if (package.PackageSourceType == Compatibility.Common.Model.PackageSourceType.NUGET)
                    {
                        packagesOnlyNeedToCheck.Add(package);
                    }

                    var apis = request.PackageWithApis[package];
                    foreach (var api in apis)
                    {
                        // TODO: HasCachedApiAnalysisResult
                        if (_cacheManager.CacheExists(package, api, targetFramework))
                        {
                            AddCachedApiAnalysisToResult(package, api, _cacheManager.Get(package, api, targetFramework), ref apiAnalysisResultsDic);
                        }
                        else
                        {
                            if (packageWithApisNeedToCheck.ContainsKey(package))
                            {
                                var apiList = packageWithApisNeedToCheck[package];
                                apiList.Add(api);
                                packageWithApisNeedToCheck[package] = apiList;
                            }
                            else
                            {
                                packageWithApisNeedToCheck.Add(package, new HashSet<ApiEntity>() { api });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"fail to apply cache result on package {package}");
                }
            }
            
            // merge packagesNeedToCheck with packageWithApisNeedToCheck
            foreach (var p in packagesOnlyNeedToCheck)
            {
                if (!packageWithApisNeedToCheck.ContainsKey(p))
                {
                    packageWithApisNeedToCheck.Add(p, new HashSet<ApiEntity>());
                }
            }
        }

        private void AddCachedApiAnalysisToResult(Compatibility.Common.Model.PackageVersionPair package, ApiEntity api, AnalysisResult cachedApiAnalysisResult,
            ref Dictionary<Compatibility.Common.Model.PackageVersionPair, Dictionary<string, AnalysisResult>> apiAnalysisResultsDic)
        {
            try
            {
                if (apiAnalysisResultsDic.ContainsKey(package))
                {
                    var apiAnalysisDic = apiAnalysisResultsDic[package];
                    apiAnalysisDic.TryAdd(api.OriginalDefinition, cachedApiAnalysisResult);
                    apiAnalysisResultsDic[package] = apiAnalysisDic;
                }
                else
                {
                    apiAnalysisResultsDic.Add(package, new Dictionary<string, AnalysisResult> { { api.OriginalDefinition, cachedApiAnalysisResult } });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"fail to add cached API analysis result {package}, api signature {api.OriginalDefinition}");
            }
        }

        public void UpdateCacheInLocal(CompatibilityCheckerResponse response, string targetFramework)
        {
            try
            {
                if (string.IsNullOrEmpty(CacheFilePath))
                {
                    _logger.LogError("No cache file path has been defined. ");
                    return;
                }
                if (response.PackageAnalysisResults == null)
                {
                    _logger.LogInformation("Empty PackageAnalysisResults in CompatibilityCheckerResponse return");
                    return;
                }

                // Update Package Analysis Result
                foreach (var pkgVersionAnalysisResult in response.PackageAnalysisResults)
                {
                    var pkgVersionPair = pkgVersionAnalysisResult.Key;
                    var pkgAnalysisResult = pkgVersionAnalysisResult.Value;
                    var compatibilityResults = pkgAnalysisResult?.CompatibilityResults;
                    var recommendations = response.GetRecommendationsForPackage(pkgVersionPair);

                    var analysisResult = new AnalysisResult
                    {
                        CompatibilityResults = compatibilityResults,
                        Recommendations = recommendations
                    };

                    // Add to cache
                    _cacheManager.Add(pkgVersionPair, targetFramework, analysisResult);
                }

                // Update Api Analysis Result
                foreach (var result in response.ApiAnalysisResults)
                {
                    foreach (var apiResult in result.Value)
                    {
                        var pkgVersionPair = result.Key;
                        var methodSignature = apiResult.Key;
                        var apiAnalysisResult = apiResult.Value;
                        var recommendations = response.GetRecommendationsForApi(pkgVersionPair, methodSignature);
                        
                        apiAnalysisResult.Recommendations = recommendations;

                        // Add to cache
                        _cacheManager.Add(pkgVersionPair, targetFramework, methodSignature, apiAnalysisResult);
                    }
                }

                var saveCacheSuccess = _cacheManager.TrySaveCacheObjectToLocalFile(CacheFilePath, out Exception ex);
                if (!saveCacheSuccess)
                {
                    _logger.LogError(ex, $"Failed to save updated cache object to file: {CacheFilePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to call UpdateCacheInLocal function on {CacheFilePath}");
            }
        }
    }
}
