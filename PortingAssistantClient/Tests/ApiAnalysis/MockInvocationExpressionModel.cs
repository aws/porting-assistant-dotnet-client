using AwsCodeAnalyzer.Model;

namespace Tests.Analysis
{
    public class MockInvocationExpressionModel : InvocationExpression
    {
        public MockInvocationExpressionModel(string originalDefinition, string namespaceName, string assembly)
        {
            SemanticOriginalDefinition = originalDefinition;
            SemanticNamespace = namespaceName;
            Reference = new Reference
            {
                Assembly = assembly
            };
            TextSpan = new TextSpan();
        }
    }
}
