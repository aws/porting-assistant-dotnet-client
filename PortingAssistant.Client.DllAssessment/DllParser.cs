using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;

namespace PortingAssistant.Client.DllAssessment;

public class DllParser
{
    public class DllInfo
    {
        public DllInfo()
        {
            methods = new List<MethodInfo>();
        }

        public List<MethodInfo?> methods;
        public string dllName;
    }

    public class MethodInfo
    {
        public string nameSpace;
        public string className;
        public string methodName;
        public List<string> parameters;
        public string returnValue;
        public string modifiers;
        public string signature;
    }

    public static List<DllInfo> ReadDLL(string dllPath)
    {
        List<DllInfo> nsList = new List<DllInfo>();

        var reference = MetadataReference.CreateFromFile(dllPath);

        var compilation = CSharpCompilation.Create(null).AddReferences(reference);

        var assemblySymbol = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(reference);

        void Write(INamespaceOrTypeSymbol symbol, List<DllInfo> nsList)
        {
            foreach (var memberSymbol in symbol.GetMembers().OfType<INamespaceOrTypeSymbol>())
            {
                var dllInfo = new DllInfo
                {
                    dllName = dllPath,
                    methods = memberSymbol.GetMembers().OfType<IMethodSymbol>().Select(method =>
                        {
                            if (method.DeclaredAccessibility.Equals(Accessibility.Private) ||
                                method.DeclaredAccessibility.Equals(Accessibility.ProtectedAndInternal) ||
                                method.DeclaredAccessibility.Equals(Accessibility.Internal))
                            {
                                return null;
                            }
                            return new MethodInfo
                            {
                                methodName = method.Name.ToString(),
                                returnValue = method.ReturnType.ToString() ,
                                parameters = method.Parameters.Select((param) => param.ToString()).ToList(),
                                modifiers = method.DeclaredAccessibility.ToString(),
                                signature = method.ToString(),
                                nameSpace = method.ContainingNamespace.ToString(),
                                className = method.ContainingType.ToString()
                            };
                        }).Where((method) => method != null).ToList()
                };
                nsList.Add(dllInfo);

                Write(memberSymbol, nsList);
            }
        }

        Write(assemblySymbol.GlobalNamespace, nsList);

        return nsList;
    }
}