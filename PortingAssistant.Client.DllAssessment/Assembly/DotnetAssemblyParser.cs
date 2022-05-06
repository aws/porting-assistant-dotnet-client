using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AwsEncoreService.Compatibility.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AwsEncoreService.Compatibility.Handler
{
    public class DotnetAssemblyParser
    {
        private string _filePath;
        private string _fileName;
        private DotnetAssemblyModel _assemblyResult;
        public DotnetAssemblyParser(string dllPath, string packagePath)
        {
            _filePath = Path.Combine(packagePath, dllPath);
            _fileName = Path.GetFileName(_filePath);

            _assemblyResult = new DotnetAssemblyModel();
            _assemblyResult.DllPath = dllPath;
            _assemblyResult.DllName = _fileName;
        }

        public async Task<DotnetAssemblyModel> Parse()
        {
            var reference = MetadataReference.CreateFromFile(_filePath);
            var compilation = CSharpCompilation.Create(null).AddReferences(reference);
            var assemblySymbol = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(reference);
            if (assemblySymbol == null || assemblySymbol.GlobalNamespace == null)
            {
                return _assemblyResult;
            }

            _assemblyResult.DllVersion = assemblySymbol.Identity.Version.ToString();
            _assemblyResult.DllName = assemblySymbol.Name.ToString();

            WalkTree(assemblySymbol.GlobalNamespace, _assemblyResult);
            return _assemblyResult;
        }

        private void WalkTree(INamespaceOrTypeSymbol symbol, DotnetAssemblyModel result)
        {
            foreach (var memberSymbol in symbol.GetMembers().OfType<INamespaceOrTypeSymbol>())
            {
                if (!memberSymbol.DeclaredAccessibility.Equals(Accessibility.Public))
                {
                    continue;
                }
                
                var methods = memberSymbol.GetMembers().OfType<IMethodSymbol>().Select((method) =>
                   {

                       if (!method.DeclaredAccessibility.Equals(Accessibility.Public))
                       {
                           return null;
                       }

                       return new MethodInfo
                       {
                           methodName = method.Name.ToString(),
                           returnValue = method.ReturnType.ToString(),
                           parameters = method.Parameters.Select((param) => param.ToString()).ToList(),
                           modifiers = method.DeclaredAccessibility.ToString(),
                           signature = method.ToString(),
                           nameSpace = method.ContainingNamespace.ToString(),
                           className = method.ContainingType.ToString()
                       };
                   }).Where((method) => method != null).ToList();


                result.Methods.AddRange(methods);

                WalkTree(memberSymbol, result);
            }
        }

    }
}
