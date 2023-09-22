using System;
using Newtonsoft.Json;
using PortingAssistant.Compatibility.Common.Interface;
using PortingAssistant.Compatibility.Common.Model;

namespace PortingAssistant.Compatibility.Core
{
	public class CacheManager : ICacheManager
        {
            //key format <targetframework>-packageVersionPairStr or  <targetframework>-packageVersionPairStr-<apiMethodSignature>
            private static readonly Dictionary<string, AnalysisResult> _cacheObject = new Dictionary<string, AnalysisResult>();

            public bool TryLoadCacheObjectFromLocalFile(string content, out Exception? exception)
            {
                exception = null;
                Clear();
                try
                {
                    var localCache = JsonConvert.DeserializeObject<Dictionary<string, AnalysisResult>>(content);
                    if (localCache != null)
                    {
                        foreach (var key in localCache?.Keys)
                        {
                            Add(key, localCache[key]);
                        }
                        return true;
                    }
                    else
                    {
                        exception = new Exception("Get null object after Deserialize Content");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                    return false;
                }
            }

            public bool TrySaveCacheObjectToLocalFile(string fileName, out Exception? exception)
            {
                exception = null;
                try
                {
                    JsonSerializerSettings _options = new() { NullValueHandling = NullValueHandling.Ignore };
                    var jsonString = JsonConvert.SerializeObject(_cacheObject, _options);
                    File.WriteAllText(fileName, jsonString);
                    return true;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    return false;
                }
            }

            public void Add(string key, AnalysisResult analysisResult)
            {
                _cacheObject.TryAdd(key, analysisResult);
            }

            public void Add(PackageVersionPair package, string targetFramework, AnalysisResult analysisResult)
            {
                var key = GenerateCacheKey(package, targetFramework);
                Add(key, analysisResult);
            }

            public void Add(PackageVersionPair package, string targetFramework, string apiMethodSignature, AnalysisResult analysisResult)
            {
                var key = $"{targetFramework}-{package.ToString()}-{apiMethodSignature}";
                Add(key, analysisResult);
            }

            public AnalysisResult Get(string key)
            {
                _ = _cacheObject.TryGetValue(key, out AnalysisResult? analysisResult);
                return analysisResult;
            }

            public bool CacheExists(PackageVersionPair package, string targetFramework)
            {
                var key = GenerateCacheKey(package, targetFramework);
                return _cacheObject.ContainsKey(key);
            }

            public bool CacheExists(PackageVersionPair package, ApiEntity apiEntity, string targetFramework)
            {
                var key = GenerateCacheKey(package, targetFramework, apiEntity);
                return _cacheObject.ContainsKey(key);
            }

            public AnalysisResult Get(PackageVersionPair package, string targetFramework)
            {
                var key = GenerateCacheKey(package, targetFramework);
                return Get(key);
            }

            public AnalysisResult Get(PackageVersionPair package, ApiEntity apiEntity, string targetFramework)
            {
                var key = GenerateCacheKey(package, targetFramework, apiEntity);
                return Get(key);
            }

            public string GenerateCacheKey(PackageVersionPair package, string targetFramework, ApiEntity apiEntity = null)
            {
                if (apiEntity == null)
                {
                    return $"{targetFramework}-{package.ToString()}";
                }
                else
                {
                    return $"{targetFramework}-{package.ToString()}-{apiEntity.OriginalDefinition}";
                }
            }

            public void Clear()
            {
                _cacheObject.Clear();
            }

            public bool DoseCacheObjectsContainData()
            {
                return _cacheObject.Any();
            }

        }
    
}

