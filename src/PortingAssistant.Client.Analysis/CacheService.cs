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
         

        public CacheService(ICacheManager cacheManager,  ILogger<CacheService> logger)
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

        //public void ValidateCacheFile(PortingAssistantConfiguration config)
        public void ValidateCacheFile(string cacheFilePath, int cacheExpirationHours = 24)
        {
            try
            {
                if (!string.IsNullOrEmpty(cacheFilePath)) {
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
            out HashSet<Compatibility.Common.Model.PackageVersionPair> packagesNeedToCheck,
            out Dictionary<Compatibility.Common.Model.PackageVersionPair, HashSet<ApiEntity>> packageWithApisNeedToCheck,
            ref Dictionary<Compatibility.Common.Model.PackageVersionPair, AnalysisResult> packageAnalysisResultsDic,
            ref Dictionary<Compatibility.Common.Model.PackageVersionPair, Dictionary<string, AnalysisResult>> apiAnalysisResultsDic)
        {
            var targetFramework = request.TargetFramework;
            packagesNeedToCheck = new HashSet<Compatibility.Common.Model.PackageVersionPair>();
            packageWithApisNeedToCheck = new Dictionary<Compatibility.Common.Model.PackageVersionPair, HashSet<ApiEntity>>();

            //load results from cache object

            foreach (var package in allPackages)
            {
                try
                {
                    if (_cacheManager.CacheExists(package, targetFramework))
                    {
                        packageAnalysisResultsDic.Add(package, _cacheManager.Get(package, targetFramework));
                        _logger.LogInformation($"{package} exist in package cache");
                    }
                    else if (package.PackageSourceType == Compatibility.Common.Model.PackageSourceType.NUGET)
                    {
                        packagesNeedToCheck.Add(package);
                    }

                    var apis = request.PackageWithApis[package];
                    foreach (var api in apis)
                    {
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
                if (string.IsNullOrEmpty(CacheFilePath) )
                {
                    _logger.LogError("No cache file path has been defined. ");
                    return;
                }
                if (response.PackageAnalysisResults == null)
                {
                    _logger.LogInformation("Empty PackageAnalysisResults in CompatibilityCheckerResponse return");
                    return;
                }
                //update Package Analysis Result
                foreach (var p in response.PackageAnalysisResults)
                {
                    var analysisResult = new AnalysisResult()
                    {
                        CompatibilityResults = p.Value?.CompatibilityResults,
                        Recommendations = p.Value?.Recommendations
                    };

                    //add to Cache
                    _cacheManager.Add(p.Key, targetFramework, analysisResult);
                }

                foreach (var result in response.ApiAnalysisResults)
                {
                    foreach (var apiResult in result.Value)
                    {
                        _cacheManager.Add(result.Key, targetFramework, apiResult.Key, apiResult.Value);
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

