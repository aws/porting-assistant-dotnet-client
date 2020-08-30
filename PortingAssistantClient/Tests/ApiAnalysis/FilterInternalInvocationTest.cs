using System.Collections.Generic;
using System.IO;
using AwsCodeAnalyzer.Model;
using EncoreApiAnalysis.Utils;
using EncoreCommon.Model;
using NUnit.Framework;

namespace Tests.ApiAnalysis
{
    public class FilterInternalInvocationTest
    {
        [Test]
        public void TestNoMatch()
        {
            var allInvocations = new List<InvocationExpression>
            {
                new MockInvocationExpressionModel("definition1", "namespace1")
            };

            var project = new Project
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
                new MockInvocationExpressionModel("definition1", "namespace1"),
                new MockInvocationExpressionModel("definition1", "namespace1.anothernamespace"),
                new MockInvocationExpressionModel("definition2", "namespace2"),
                new MockInvocationExpressionModel("definition2", "namespace2.anothernamespace")
            };

            var project = new Project
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
                new MockInvocationExpressionModel("definition1", "namespace1"),
            };

            var project = new Project
            {
                ProjectName = "namespace1",
                ProjectReferences = new List<ProjectReference>()
            };

            var invocations = FilterInternalInvocations.Filter(allInvocations, project);
            Assert.AreEqual(0, invocations.Count);
        }
    }
}
