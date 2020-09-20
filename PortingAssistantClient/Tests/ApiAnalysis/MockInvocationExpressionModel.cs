using AwsCodeAnalyzer.Model;

namespace Tests.Analysis
{
    public class MockInvocationExpressionModel : InvocationExpression
    {
        public MockInvocationExpressionModel(string originalDefinition, string namespaceName)
        {
            SemanticOriginalDefinition = originalDefinition;
            SemanticNamespace = namespaceName;
            TextSpan = new TextSpan();
        }
    }
}
