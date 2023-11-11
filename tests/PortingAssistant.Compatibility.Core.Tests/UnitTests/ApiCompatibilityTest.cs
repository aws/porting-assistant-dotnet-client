using NUnit.Framework;
using PortingAssistant.Compatibility.Common.Model;
using PortingAssistant.Compatibility.Common.Utils;
using Constants = PortingAssistant.Compatibility.Common.Utils.Constants;

namespace PortingAssistant.Compatibility.Core.Tests.UnitTests
{
    public class ApiCompatibilityTest
    {
        private string DEFAULT_TARGET = Constants.DefaultAssessmentTargetFramework;

        // package IsDeprecated is true.
        private readonly PackageDetails _packageDetails = new PackageDetails
        {
            Name = "Newtonsoft.Json",
            Versions = new SortedSet<string> { "12.0.3", "12.0.4", "13.0.2" },
            Api = new ApiDetails[]
            {
                new ApiDetails
                {
                    MethodName = "Setup(Object)",
                    MethodSignature = "Newtonsoft.Json.JsonConvert.SerializeObject(object)",
                    Targets = new Dictionary<string, SortedSet<string>>
                    {
                        {
                             "netcoreapp3.1", new SortedSet<string> { "10.2.0", "12.0.3", "12.0.4", "13.0.2" }
                        },
                        {
                             "net6.0", new SortedSet<string> { "10.2.0", "12.0.3", "12.0.4", "13.0.2" }
                        }
                    },
                },
                new ApiDetails
                {
                    MethodName = "SerializeObject",
                    MethodSignature = "Public Shared Overloads Function SerializeObject(value As Object) As String",
                    Targets = new Dictionary<string, SortedSet<string>>
                    {
                        {
                             "netcoreapp3.1", new SortedSet<string> { "10.2.0", "12.0.3", "12.0.4", "13.0.2" }
                        },
                        {
                             "net6.0", new SortedSet<string> { "10.2.0", "12.0.3", "12.0.4", "13.0.2" }
                        }
                    },
                }
            },
            Targets = new Dictionary<string, SortedSet<string>> {
                {
                    "netcoreapp3.1",
                    new SortedSet<string> { "12.0.3", "12.0.4", "13.0.2" }
                },
                {
                    "net6.0",
                    new SortedSet<string> { "12.0.3", "12.0.4", "13.0.2" }
                }
            },
            License = new LicenseDetails
            {
                License = new Dictionary<string, SortedSet<string>>
                {
                    { "MIT", new SortedSet<string> { "12.0.3", "12.0.4", "13.0.2" } }
                }
            },
            Namespaces = new string[] { "TestNamespace" },
            //IsDeprecated = true
        };

        [Test]
        public void GetCompatibilityResult_PackageIsDeprecated()
        {
            _packageDetails.IsDeprecated = true;
            Dictionary<string, int> indexDict = new Dictionary<string, int>()
            {
                {"test",0}
            };

            var packageDetailsWithApiIndices = new PackageDetailsWithApiIndices()
            {
                PackageDetails = _packageDetails,
                IndexDict = indexDict,
            };

            var apiEntity = new ApiEntity()
            {
                CodeEntityType = CodeEntityType.Class,
                Namespace = "",
                OriginalDefinition = "TestOriginalDefinition"
            };

            string packageVersion = "4.0.0";

            var result = ApiCompatiblity.GetCompatibilityResult(packageDetailsWithApiIndices, apiEntity,
                packageVersion, DEFAULT_TARGET, false);

            Assert.AreEqual(Common.Model.Compatibility.DEPRECATED, result.Compatibility);
        }

        [Test]
        public void GetCompatibilityResult_PackageIsUnknown()
        {
            Dictionary<string, int> indexDict = new Dictionary<string, int>()
            {
                {"test",0}
            };

            var packageDetailsWithApiIndices = new PackageDetailsWithApiIndices()
            {
                PackageDetails = _packageDetails,
                IndexDict = indexDict,
            };

            var apiEntity = new ApiEntity()
            {
                CodeEntityType = CodeEntityType.Method,
                Namespace = "",
                OriginalDefinition = "TestOriginalDefinition"
            };

            string packageVersion = "4.0.0";

            var result = ApiCompatiblity.GetCompatibilityResult(packageDetailsWithApiIndices, apiEntity,
                packageVersion, DEFAULT_TARGET, false);

            Assert.AreEqual(Common.Model.Compatibility.UNKNOWN, result.Compatibility);
        }

        [Test]
        public void GetCompatibilityResult_Incompatible()
        {
            _packageDetails.IsDeprecated = false;
            Dictionary<string, int> indexDict = new Dictionary<string, int>()
            {
                {"test",0}
            };

            var packageDetailsWithApiIndices = new PackageDetailsWithApiIndices()
            {
                PackageDetails = _packageDetails,
                IndexDict = indexDict,
            };

            var apiEntity = new ApiEntity()
            {
                CodeEntityType = CodeEntityType.Class,
                Namespace = "Test",
                OriginalDefinition = "TestOriginalDefinition"
            };

            string packageVersion = "4.0.0";
            string target = "1";

            var result = ApiCompatiblity.GetCompatibilityResult(packageDetailsWithApiIndices, apiEntity,
                packageVersion, target, false);

            Assert.AreEqual(Common.Model.Compatibility.INCOMPATIBLE, result.Compatibility);
        }

        [Test]
        public void GetApiCompatibilityResult_IncompatiblePackageAndCompatibleSdk_ReturnCompatibleSdk()
        {
            CompatibilityResult compatibilityResultWithPackage = new CompatibilityResult()
            {
                Compatibility = Common.Model.Compatibility.INCOMPATIBLE
            };
            CompatibilityResult compatibilityResultWithSdk = new CompatibilityResult()
            {
                Compatibility = Common.Model.Compatibility.COMPATIBLE
            };

            var compatiblityResult =
                ApiCompatiblity.GetApiCompatibilityResult(compatibilityResultWithPackage, compatibilityResultWithSdk);

            Assert.AreEqual(compatiblityResult, compatibilityResultWithSdk);
        }

        [Test]
        public void GetApiCompatibilityResult_DeprecatedPackageAndCompatibleSdk_ReturnCompatibleSdk()
        {
            CompatibilityResult compatibilityResultWithPackage = new CompatibilityResult()
            {
                Compatibility = Common.Model.Compatibility.DEPRECATED
            };
            CompatibilityResult compatibilityResultWithSdk = new CompatibilityResult()
            {
                Compatibility = Common.Model.Compatibility.COMPATIBLE
            };

            var compatiblityResult =
                ApiCompatiblity.GetApiCompatibilityResult(compatibilityResultWithPackage, compatibilityResultWithSdk);

            Assert.AreEqual(compatiblityResult, compatibilityResultWithSdk);
        }

        [Test]
        public void GetApiCompatibilityResult_UnknownPackageAndCompatibleSdk_ReturnCompatibleSdk()
        {
            CompatibilityResult compatibilityResultWithPackage = new CompatibilityResult()
            {
                Compatibility = Common.Model.Compatibility.UNKNOWN
            };
            CompatibilityResult compatibilityResultWithSdk = new CompatibilityResult()
            {
                Compatibility = Common.Model.Compatibility.COMPATIBLE
            };

            var compatiblityResult =
                ApiCompatiblity.GetApiCompatibilityResult(compatibilityResultWithPackage, compatibilityResultWithSdk);

            Assert.AreEqual(compatiblityResult, compatibilityResultWithSdk);
        }

        [Test]
        public void GetApiDetails_NullPackageDetails_ReturnNull()
        {
            Dictionary<string, int> indexDict = new Dictionary<string, int>()
            {
                {"test",0}
            };

            var packageDetailsWithApiIndices = new PackageDetailsWithApiIndices()
            {
                PackageDetails = null,
                IndexDict = indexDict,
            };

            string apiMethodSignature = "test";

            var result = ApiCompatiblity.GetApiDetails(packageDetailsWithApiIndices, apiMethodSignature);

            Assert.IsNull(result);
        }

        [Test]
        public void PreProcessPackageDetails_WithCanceledTask_ReturnNull()
        {
            var canceledTask = Task.FromCanceled<PackageDetails>(new System.Threading.CancellationToken(true));
            var packageResults = new Dictionary<PackageVersionPair, Task<PackageDetails>>
            {
                { new PackageVersionPair(), canceledTask }
            };

            var result = ApiCompatiblity.PreProcessPackageDetails(packageResults);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void PreProcessPackageDetails_CatchException_ReturnsEmptyDictionary()
        {
            var packageResults = new Dictionary<PackageVersionPair, Task<PackageDetails>>
            {
            };

            var result = ApiCompatiblity.PreProcessPackageDetails(packageResults);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }
        
        [TestCase("Microsoft.AspNetCore.Hosting.IHostingEnvironment", "Microsoft.AspNetCore.Hosting.IHostingEnvironment")]
        [TestCase("Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment", "Microsoft.AspNetCore.Hosting.IHostingEnvironment")]
        [TestCase("System.Collections.Generic.IReadOnlyDictionary<string, T>", "System.Collections.Generic.IReadOnlyDictionary<string, T>")]
        [TestCase("System.Collections.Generic.IReadOnlyDictionary<string, T> SomeDict", "System.Collections.Generic.IReadOnlyDictionary<string, T>")]
        [TestCase("System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>", "System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>")]
        [TestCase("System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> SomeFunc", "System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>")]
        [TestCase("System.Func<Microsoft.AspNetCore.Mvc.ActionContext, Microsoft.AspNetCore.Mvc.IActionResult> value", "System.Func<Microsoft.AspNetCore.Mvc.ActionContext, Microsoft.AspNetCore.Mvc.IActionResult>")]
        [TestCase("object[] value", "object[]")]
        [TestCase("out System.Net.IPEndPoint? result", "out System.Net.IPEndPoint?")]
        [TestCase("params string[] include", "params string[]")]
        [TestCase("params System.Linq.Expressions.Expression<System.Func<TModel, object>>[] includeExpressions", "params System.Linq.Expressions.Expression<System.Func<TModel, object>>[]")]
        [TestCase("ref bool initialized", "ref bool")]
        public void RemoveParameterName_Removes_Parameter_Names(
            string methodParameter,
            string expectedResult)
        {
            var actualResult = ApiCompatiblity.RemoveParameterName(methodParameter);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestCase(
            "CloneIfDifferentComparer", 
            "System.Collections.Generic.IReadOnlyDictionary<string, T>",
            "System.StringComparer", 
            "Microsoft.TemplateEngine.Utils.DictionaryExtensions.CloneIfDifferentComparer<T>(System.Collections.Generic.IReadOnlyDictionary<string, T>, System.StringComparer)",
            "System.Collections.Generic.IReadOnlyDictionary<string, T>.CloneIfDifferentComparer<T>(System.StringComparer)")]
        [TestCase(
            "CloneIfDifferentComparer",
            "System.Collections.Generic.IReadOnlyDictionary<string, T> someDict",
            "System.StringComparer someComparer",
            "Microsoft.TemplateEngine.Utils.DictionaryExtensions.CloneIfDifferentComparer<T>(System.Collections.Generic.IReadOnlyDictionary<string, T>, System.StringComparer)",
            "System.Collections.Generic.IReadOnlyDictionary<string, T>.CloneIfDifferentComparer<T>(System.StringComparer)")]
        public void GetExtensionSignature_Returns_FormattedExtension_Without_ParameterNames(string methodName, string parameter1, string parameter2, string methodSignature, string? expectedResult)
        {
            var apiDetails = new ApiDetails
            {
                MethodName = methodName,
                MethodParameters = new [] { parameter1, parameter2},
                MethodSignature = methodSignature
            };

            var actualResult = ApiCompatiblity.GetExtensionSignature(apiDetails);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestCase(
            "CloneIfDifferentComparer",
            "System.Collections.Generic.IReadOnlyDictionary<string, T>",
            "System.StringComparer",
            "Microsoft.TemplateEngine.Utils.DictionaryExtensions.CloneIfDifferentComparer<T>(System.Collections.Generic.IReadOnlyDictionary<string, T>, System.StringComparer)",
            "System.Collections.Generic.IReadOnlyDictionary<string, T>.CloneIfDifferentComparer<T>(System.StringComparer)")]
        [TestCase(
            "CloneIfDifferentComparer",
            "System.Collections.Generic.IReadOnlyDictionary<string, T> someDict",
            "System.StringComparer someComparer",
            "Microsoft.TemplateEngine.Utils.DictionaryExtensions.CloneIfDifferentComparer<T>(System.Collections.Generic.IReadOnlyDictionary<string, T>, System.StringComparer)",
            "System.Collections.Generic.IReadOnlyDictionary<string, T>.CloneIfDifferentComparer<T>(System.StringComparer)")]
        public void GetExtensionSignature_Returns_FormattedExtension_Without_ParameterNames_ForV2(string methodName, string parameter1, string parameter2, string methodSignature, string? expectedResult)
        {
            var apiDetails = new ApiDetailsV2
            {
                methodName = methodName,
                methodParameters = new [] { parameter1, parameter2 },
                methodSignature = methodSignature
            };

            var actualResult = ApiCompatiblity.GetExtensionSignature(apiDetails);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [Test]
        public void GetExtensionSignature_Returns_Null_When_NoParametersArePassed()
        {
            var apiDetails = new ApiDetails
            {
                MethodName = "AnyMethod",
                MethodParameters = new string[] { },
                MethodSignature = "AnySignature"
            };

            var actualResult = ApiCompatiblity.GetExtensionSignature(apiDetails);
            Assert.AreEqual(null, actualResult);
        }

        [Test]
        public void GetExtensionSignature_Returns_Null_When_NoParametersArePassed_ForV2()
        {
            var apiDetails = new ApiDetailsV2()
            {
                methodName = "AnyMethod",
                methodParameters = new string[] { },
                methodSignature = "AnySignature"
            };

            var actualResult = ApiCompatiblity.GetExtensionSignature(apiDetails);
            Assert.AreEqual(null, actualResult);
        }
    }
}
