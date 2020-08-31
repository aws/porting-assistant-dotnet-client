using System;
namespace PortingAssistant.Model
{
    public class PackageAnalysisResult
    {
        public PackageVersionPair PackageVersionPair { get; set; }
        public Compatibility CompatibilityResult { get; set; }
        public PackageRecommendation PackageRecommendation { get; set; }
    }
}
