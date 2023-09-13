using Amazon.Runtime.Internal.Transform;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codelyzer.Analysis.Model;
using PortingAssistant.Compatibility.Common.Model;

namespace PortingAssistant.Client.Analysis.Mappers
{
    public class PackageAnalysisResultsMapper
    {
        public static Dictionary<PortingAssistant.Client.Model.PackageVersionPair, Task<PortingAssistant.Client.Model.PackageAnalysisResult>> Convert(Dictionary<PortingAssistant.Compatibility.Common.Model.PackageVersionPair, AnalysisResult> packageAnalysisResults)
        {
            
            Dictionary<PortingAssistant.Client.Model.PackageVersionPair, Task<PortingAssistant.Client.Model.PackageAnalysisResult>> result
                = new Dictionary<PortingAssistant.Client.Model.PackageVersionPair, Task<PortingAssistant.Client.Model.PackageAnalysisResult>>();
            if (packageAnalysisResults == null)
            {
                return result;
            }
            foreach (var r in packageAnalysisResults)
            {
                var package = PackageVersionPairMapper.Convert(r.Key);
                var task = new PortingAssistant.Client.Model.PackageAnalysisResult()
                {
                    
                    Recommendations = new PortingAssistant.Client.Model.Recommendations { RecommendedActions = RecommandationMapper.ConvertToPackageRecommendation(r.Value?.Recommendations?.RecommendedActions) },
                    PackageVersionPair = package,
                    CompatibilityResults = CompatibilityResultMapper.Convert(r.Value.CompatibilityResults)
                };
                
                result.Add(package, GetPackageAnalysisResultTask(task));
            }
            return result;
        }

        private static async Task<PortingAssistant.Client.Model.PackageAnalysisResult> GetPackageAnalysisResultTask(PortingAssistant.Client.Model.PackageAnalysisResult result)
        {
            await Task.Run(() => { });
            return result;
        }
    }
}
