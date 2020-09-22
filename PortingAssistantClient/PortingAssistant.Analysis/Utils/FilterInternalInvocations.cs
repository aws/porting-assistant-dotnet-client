using System.Collections.Generic;
using System.IO;
using System.Linq;
using AwsCodeAnalyzer.Model;
using PortingAssistant.Model;

namespace PortingAssistant.Analysis.Utils
{
    public static class FilterInternalInvocations
    {
        public static List<InvocationExpression> Filter(List<InvocationExpression> allInvocations, ProjectDetails project)
        {
            var projectReferences = project.ProjectReferences.Select((references) =>
            {
                return Path.GetFileNameWithoutExtension(references.ReferencePath);
            }).ToHashSet();

            projectReferences.Add(Path.GetFileNameWithoutExtension(project.ProjectName));

            var namespaces = InvocationFilterData.Namespaces;

            return allInvocations.Where(invocation =>
            {
                return invocation.SemanticOriginalDefinition != null &&
                    projectReferences.Where(r => invocation.SemanticNamespace.StartsWith(r)).Count() == 0 &&
                    !namespaces.Contains(invocation.SemanticNamespace);
            }).ToList();
        }
    }
}
