using System;
using PortingAssistant.Compatibility.Common.Model;

namespace PortingAssistant.Compatibility.Common.Interface
{
	public interface ICacheManager
    {
        void Add(string key, AnalysisResult analysisResult);
        //public void Add(Tuple<string, string> packageVersionPair_methodSiganiture, AnalysisResult analysisResult);
        AnalysisResult Get(string key);
        bool CacheExists(PackageVersionPair package, string targetFramework);
        AnalysisResult Get(PackageVersionPair package, string targetFramework);
        bool CacheExists(PackageVersionPair package, ApiEntity apiEntity, string targetFramework);

        AnalysisResult Get(PackageVersionPair package, ApiEntity apiEntity, string targetFramework);
        void Add(PackageVersionPair package, string targetFramework, AnalysisResult analysisResult);
        void Add(PackageVersionPair package, string targetFramework, string apiMethodSignature, AnalysisResult analysisResult);
        void Clear();

        bool TryLoadCacheObjectFromLocalFile(string content, out Exception? exception);
        bool TrySaveCacheObjectToLocalFile(string fileName, out Exception? exception);
        public bool DoseCacheObjectsContainData();
    }
    
}

