using System.Collections.Generic;
using System.IO;
using AwsCodeAnalyzer.Model;
using PortingAssistant.Analysis.Utils;
using PortingAssistant.Model;
using NUnit.Framework;

namespace Tests.Analysis
{
    public class FilterInternalInvocationTest
    {
        [Test]
        public void TestNoMatch()
        {
            var allInvocations = new List<InvocationExpression>
            {
                new MockInvocationExpressionModel("definition1", "namespace1", "test")
            };

            var project = new ProjectDetails
            {
                ProjectName = "namespace3.csproj",
                ProjectReferences = new List<ProjectReference>
                {
                    new ProjectReference {
                        ReferencePath = Path.Join("some", "path", "namespace2.csproj")
                    }
                }
            };

            var invocations = FilterInternalInvocations.Filter(allInvocations, project);
            Assert.AreEqual(1, invocations.Count);
        }

        [Test]
        public void TestFilterProject()
        {
            var allInvocations = new List<InvocationExpression>
            {
                new MockInvocationExpressionModel("definition1", "namespace1", "test"),
                new MockInvocationExpressionModel("definition1", "namespace1.anothernamespace", "test1"),
                new MockInvocationExpressionModel("definition2", "namespace2", "test2"),
                new MockInvocationExpressionModel("definition2", "namespace2.anothernamespace", "test2")
            };

            var project = new ProjectDetails
            {
                ProjectName = "namespace1.csproj",
                ProjectReferences = new List<ProjectReference>
                {
                    new ProjectReference {
                        ReferencePath = Path.Join("some", "path", "namespace2.csproj")
                    }
                }
            };

            var invocations = FilterInternalInvocations.Filter(allInvocations, project);
            Assert.AreEqual(0, invocations.Count);
        }

        [Test]
        public void TestFilterSelfProject()
        {
            var allInvocations = new List<InvocationExpression>
            {
                new MockInvocationExpressionModel("definition1", "namespace1", "test"),
            };

            var project = new ProjectDetails
            {
                ProjectName = "namespace1",
                ProjectReferences = new List<ProjectReference>()
            };

            var invocations = FilterInternalInvocations.Filter(allInvocations, project);
            Assert.AreEqual(0, invocations.Count);
        }
    }
}
