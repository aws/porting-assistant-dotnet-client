using PortingAssistant.Client.Common.Model;
using PortingAssistant.Client.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Client.Common.Utils
{
    public static class AnalysisUtils
    {
        public static ProjectCompatibilityResult GenerateCompatibilityResults(List<SourceFileAnalysisResult> sourceFileAnalysisResults, string projectPath, bool isPorted)
        {
            var projectCompatibilityResult = new ProjectCompatibilityResult() { IsPorted = isPorted, ProjectPath = projectPath };

            sourceFileAnalysisResults.ForEach(SourceFileAnalysisResult =>
            {
                SourceFileAnalysisResult.ApiAnalysisResults.ForEach(apiAnalysisResult =>
                {
                    var currentEntity = projectCompatibilityResult.CodeEntityCompatibilityResults.First(r => r.CodeEntityType == apiAnalysisResult.CodeEntityDetails.CodeEntityType);

                    var hasAction = SourceFileAnalysisResult.RecommendedActions.Any(ra => ra.TextSpan.Equals(apiAnalysisResult.CodeEntityDetails.TextSpan));
                    if (hasAction)
                    {
                        currentEntity.Actions++;
                    }
                    var compatibility = apiAnalysisResult.CompatibilityResults?.FirstOrDefault().Value?.Compatibility;
                    if (compatibility == Compatibility.COMPATIBLE)
                    {
                        currentEntity.Compatible++;
                    }
                    else if (compatibility == Compatibility.INCOMPATIBLE)
                    {
                        currentEntity.Incompatible++;
                    }
                    else if (compatibility == Compatibility.UNKNOWN)
                    {
                        currentEntity.Unknown++;

                    }
                    else if (compatibility == Compatibility.DEPRECATED)
                    {
                        currentEntity.Deprecated++;
                    }
                    else
                    {
                        currentEntity.Unknown++;
                    }
                });
            });

            return projectCompatibilityResult;
        }

    }
}
