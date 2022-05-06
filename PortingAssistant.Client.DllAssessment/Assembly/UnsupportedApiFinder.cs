using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NuGet.Packaging;

namespace AwsEncoreService.Compatibility.Handler
{
    public class UnsupportedApiFinder
    {
        private const string Reference = "?";
        
        private readonly ModuleDefinition _moduleDefinition;
        private readonly ISet<string> UNSUPPORTED_EXCEPTIONS;
        private readonly Dictionary<string, string> _methodsIL2CSharpMap;

        // Public unsupported methods
        private ISet<string> _publicUnsupportedMethods;
        private int _unsupportedMethodsCount = 0;
        private readonly bool _isDebug = false;

        public UnsupportedApiFinder(string assemblyFilePath, IEnumerable<string>? searchDirectories = null)
        {
            var resolver = new DefaultAssemblyResolver();

            // Enable resolution of local assemblies
            resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyFilePath));

            // Enable resolution of any other assemblies
            if (searchDirectories != null)
            {
                foreach (var directory in searchDirectories)
                {
                    resolver.AddSearchDirectory(directory);
                }
            }

            _moduleDefinition = ModuleDefinition.ReadModule(assemblyFilePath, new
                ReaderParameters { AssemblyResolver = resolver });

            var incompatibleMethod = _moduleDefinition
                .Types
                .First(t => t.Name.Contains("TestController"))
                .Methods
                .First(m => m.Name.Contains("Incompatible"));


            UNSUPPORTED_EXCEPTIONS = new HashSet<string>
            {
                "System.Void System.PlatformNotSupportedException::.ctor()",
                "System.Void System.NotSupportedException::.ctor(System.String)"
            };

            _methodsIL2CSharpMap = new Dictionary<string, string>();
            _publicUnsupportedMethods = new HashSet<string>();
        }

        public bool IsUnsupportedApi(string methodSignature)
        {
            var signature = NormalizeMethodSignature(methodSignature);

            return _publicUnsupportedMethods.Contains(signature);
        }

        public bool IsApiDefined(string methodSignature)
        {
            var signature = NormalizeMethodSignature(methodSignature);

            return _methodsIL2CSharpMap.ContainsValue(signature);
        }

        private string NormalizeMethodSignature(string methodSignature)
        {
            var signature = methodSignature;
            
            /* Handle Nullable parameters containing '?' */
            if (signature.Contains(Reference))
            {
                signature = signature.Replace(Reference, "");
            }
            
            if (methodSignature.Contains(ILTypeMapper.SystemDecimal))
            {
                signature = signature.Replace(ILTypeMapper.SystemDecimal, 
                    ILTypeMapper.SystemDecimalShort);
            }

            return signature;
        }

        public void ComputeUnsupportedApis()
        {
            foreach (TypeDefinition klass in _moduleDefinition.Types)
            { 
                try
                {
                    TraverseAllClasses(klass, new HashSet<string>());
                }
                catch (AssemblyResolutionException e)
                {
                    // This exception will occur when a referenced API belongs to a dependency that is not packaged with the nuget package
                    // (in Visual Studio, these dependencies will be automatically resolved but there is no known way of detecting these
                    // dependencies using the Nuget.Client package).
                    // This is quite common so logging the message is commented out to prevent excess log growth. Logging can be re-enabled
                    // for debugging purposes.
                    //Console.WriteLine($"Failed to resolve dependency used by class {klass}. Dependency may have been excluded from the package: {e.Message}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            if (_isDebug)
            {
                Console.WriteLine("IL to CSharp methods mapping ");
                Console.WriteLine("--------------------------------------------------------------");
                
                foreach (var kv in _methodsIL2CSharpMap)
                {
                    Console.WriteLine(kv.Key + " --> " + kv.Value);
                }

                Console.WriteLine("publicUnsupportedMethods : " + _unsupportedMethodsCount);
                Console.WriteLine(string.Join("\n", _publicUnsupportedMethods));
            }
        }
        
        private void TraverseAllClasses(TypeDefinition klass, HashSet<string> visited)
        {
            /* Some methods are missing if we block this */
            if (!klass.IsPublic)
            {
                return;
            }

            if (visited.Contains(klass.FullName))
            {
                return;
            }

            AddConstuctor(klass);
            foreach (var method in klass.Methods)
            {
                TraverseAllMethods(klass, method);
            }

            //if (!klass.HasNestedTypes|| !klass.IsNested) return;
            
            foreach (var nested in klass.NestedTypes)
            {
                visited.Add(klass.FullName);
                TraverseAllClasses(nested, visited);
                visited.Remove(klass.FullName);
            }
        }

        private void AddConstuctor(TypeDefinition klass)
        {
            string cname = ILMethodHelper.ConvertToCSharpConstructor(klass);
            if (!string.IsNullOrEmpty(cname))
                _methodsIL2CSharpMap[cname] = cname;
        }
        
        private void TraverseAllMethods(TypeDefinition klass, MethodDefinition method)
        {
            /*if (!method.IsPublic)
                return;
            */
            var info = ScanPlatformNotSupported(method, new HashSet<string>(), 0);

            var cSharpMethods = ILMethodHelper.ConvertToCSharpMethod(method);

            if (_methodsIL2CSharpMap.ContainsKey(method.FullName))
            {
                _methodsIL2CSharpMap[_methodsIL2CSharpMap[method.FullName]] = cSharpMethods[0];
            }
            else
            {
                _methodsIL2CSharpMap[method.FullName] = cSharpMethods[0];
            }

            if (cSharpMethods.Count > 1)
            {
                // Sometimes we need to map two different methods: 1) Without translation 2) With translation
                // For example: Class.Method.get_Info() and Class.Method.Info.get
                _methodsIL2CSharpMap[cSharpMethods[0]] = cSharpMethods[1];
            }
            
            if (info.Level != -1)
            {
                _unsupportedMethodsCount++;
                
                _publicUnsupportedMethods.AddRange(cSharpMethods);
            }
        }

        /* Some part of the code was taken  from: https://github.com/dotnet/platform-compat/tree/master/src/ex-scan
           and modified to use  Mono.Cecil library.
         */
            private ExceptionInfo ScanPlatformNotSupported(MethodDefinition method, HashSet<string> visited, int nestingLevel = 0)
        {
            
            const int maxNestingLevel = 3;

            if (method == null || method.IsAbstract || HasIntrinsicAttribute(method))
            {
                return ExceptionInfo.DoesNotThrow;
            }
            if (visited.Contains(method.FullName)) return ExceptionInfo.DoesNotThrow;
            if (ApiExcludesLinux(method))
            {
                return ExceptionInfo.ThrowsAt(nestingLevel, method.FullName);
            }

            foreach (var op in GetOperationsPreceedingThrow(method))
            {
                // throw new PlatformNotSupportedExeption(...)
                if (op.OpCode.Code == Code.Newobj &&
                    op.Operand is MethodReference m &&
                    IsPlatformNotSupported(op.Operand.ToString()))
                {
                    return ExceptionInfo.ThrowsAt(nestingLevel, method.FullName);
                }

                // throw SomeFactoryForPlatformNotSupportedExeption(...);
                if (op.Operand is MethodReference r &&
                    IsFactoryForPlatformNotSupported(r))
                {
                    return ExceptionInfo.ThrowsAt(nestingLevel, method.FullName);
                }
            }

            var result = ExceptionInfo.DoesNotThrow;

            if (nestingLevel < maxNestingLevel)
            {
                foreach (var calledMethod in GetCalls(method.Resolve()))
                {
                    bool val = ResolveMethod(calledMethod);
                    if (!val) continue;
                    var nextMethod = calledMethod.Resolve();
                    visited.Add(nextMethod.FullName);
                    var nestedResult = ScanPlatformNotSupported(nextMethod, visited, nestingLevel + 1);
                    visited.Remove(nextMethod.FullName);
                    result = result.Combine(nestedResult);
                }
            }

            return result;
        }

        private bool ResolveMethod(MethodReference calledMethod)
        {
            try
            {
                var resolveTask = Task.Run(() => calledMethod.Resolve());
                if (resolveTask.Wait(TimeSpan.FromSeconds(2)))
                    return true;
                else
                {
                    return false;
                }
            }
            catch (AssemblyResolutionException e)
            {
                // This exception will occur when a referenced API belongs to a dependency that is not packaged with the nuget package
                // (in Visual Studio, these dependencies will be automatically resolved but there is no known way of detecting these
                // dependencies using the Nuget.Client package).
                // This is quite common so logging the message is commented out to prevent excess log growth. Logging can be re-enabled
                // for debugging purposes.
                //Console.WriteLine($"Failed to resolve dependency used by method {calledMethod}. Dependency may have been excluded from the package: {e.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine(calledMethod.FullName);
                Console.WriteLine(e);
            }
            
            return false;
        }

        private bool HasIntrinsicAttribute(MethodDefinition method)
        {
            var name = method.FullName;

            return (method.HasCustomAttributes 
                    && method.CustomAttributes.Any(attrb => 
                        attrb != null 
                        && "System.Runtime.CompilerServices.IntrinsicAttribute".Equals( attrb.AttributeType.FullName))) 
                   || name.StartsWith("System.Runtime.Intrinsics") 
                   || name.StartsWith("System.ByReference");
        }

        private static bool ApiExcludesLinux(MethodDefinition method)
        {
            if (!method.HasCustomAttributes)
            {
                return false;
            }

            var attrs = method.CustomAttributes;
            return attrs.Any(a =>
            {
                // If there is a platform inclusion list and linux is not included, return true
                if (a.AttributeType.Name == "SupportedOSPlatformAttribute"
                    && a.ConstructorArguments.All(arg => arg.Value?.ToString() != "linux"))
                {
                    return true;
                }

                // If there is a platform exclusion list and linux is included, return true
                if (a.AttributeType.Name == "UnsupportedOSPlatformAttribute"
                    && a.ConstructorArguments.Any(arg => arg.Value?.ToString() == "linux"))
                {
                    return true;
                }

                // By default, return false
                return false;
            });
        }

        private IEnumerable<Instruction> GetOperationsPreceedingThrow(MethodDefinition method)
        {
            Instruction previous = null;

            if (method.Body != null &&
                method.Body.Instructions != null)
            {

                foreach (var op in method.Body.Instructions)
                {
                    if (op.OpCode.Code == Code.Nop)
                        continue;

                    if (op.OpCode.Code == Code.Throw && previous != null)
                        yield return previous;

                    previous = op;
                }
            }
        }

        private IEnumerable<MethodReference> GetCalls(MethodDefinition method)
        {
            if (method.Body == null ||
                method.Body.Instructions == null)
            {
                return Enumerable.Empty<MethodReference>();
            }
           
            return method.Body.Instructions.Where(o => o.OpCode.Code == Code.Call ||
                                                     o.OpCode.Code == Code.Callvirt)
                                         .Select(o => o.Operand as MethodReference)
                                         .Where(m => m != null);
        }

        private bool IsPlatformNotSupported(string method)
        {
            return UNSUPPORTED_EXCEPTIONS.Contains(method);
        }

        private bool IsFactoryForPlatformNotSupported(MethodReference reference)
        {
            var methodInfo = reference.Resolve();

            if (methodInfo == null || methodInfo.IsAbstract)
                return false;

            //MethodReference constructorReference = null;
            Instruction constructorReference = null;

            foreach (var op in methodInfo.Body.Instructions)
            {
                switch (op.OpCode.Code)
                {
                    case Code.Newobj:
                        constructorReference = op;
                        break;
                    case Code.Ret:
                        if (constructorReference != null && 
                            IsPlatformNotSupported(constructorReference.Operand.ToString()))
                            return true;
                        break;
                }
            }

            return false;
        }
    }
}