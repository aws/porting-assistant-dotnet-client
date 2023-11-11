using System;
using PortingAssistant.Compatibility.Common.Model;
using System.Collections.Generic;

namespace PortingAssistant.Client.Analysis
{
    public interface ICacheService
    {
        public void ValidateCacheFile(Model.PortingAssistantConfiguration config);
        public void ValidateCacheFile(string cacheFilePath, int cacheExpirationHours = 24);
        public void ApplyCacheToCompatibleCheckerResults(CompatibilityCheckerRequest compatibilityCheckerRequest,
            List<PackageVersionPair> packageVersionPairs, 
            out Dictionary<PackageVersionPair, HashSet<ApiEntity>> packageWithApisNeedToCheck,
            ref Dictionary<PackageVersionPair, AnalysisResult> packageAnalysisResultsDic,
            ref Dictionary<PackageVersionPair, Dictionary<string, AnalysisResult>> apiAnalysisResultsDic);

        public void UpdateCacheInLocal(CompatibilityCheckerResponse response, string targetFramework);
        public bool IsCacheAvailable();
    }
}

