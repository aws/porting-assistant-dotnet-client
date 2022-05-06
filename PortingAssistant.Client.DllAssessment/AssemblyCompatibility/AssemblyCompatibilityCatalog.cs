using Mono.Cecil;
using PortingAssistant.Client.DllAssessment.AssemblyCompatibility.MetadataModels;
using PortingAssistant.Client.DllAssessment.AssemblyCompatibility.TargetFramework;

namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility;

public class AssemblyCompatibilityCatalog
{
    public Dictionary<string, NugetAssemblyMetadata> NugetAssemblies { get; set; } = new();
    public Dictionary<string, SdkAssemblyMetadata> SdkAssemblies { get; set; } = new();
    public Dictionary<string, ClassMetadata> Classes { get; set; } = new();
    public Dictionary<string, MethodMetadata> Methods { get; set; } = new();
    public Dictionary<ILInstructionMetadata, MethodMetadata> InstructionToMethodMap { get; set; } = new();

    internal void AddAssembly(ModuleDefinition moduleDefinition)
    {
        // TODO: How to separate nugets and SDKs?
        var assemblyPath = moduleDefinition.FileName;
        if (SdkAssemblies.ContainsKey(assemblyPath))
        {
            return;
        }

        var targetFramework = TargetFrameworkFinder.GetTargetFramework(moduleDefinition);
        var assemblyMetadata = new SdkAssemblyMetadata
        {
            AssemblyPath = assemblyPath,
            TargetFramework = targetFramework
        };
        SdkAssemblies.TryAdd(assemblyPath, assemblyMetadata);
    }

    internal void AddClasses(IEnumerable<TypeDefinition> types)
    {
        types.ToList().ForEach(t =>
        {
            var typeName = t.Name;
            if (Classes.ContainsKey(typeName))
            {
                return;
            }

            var classMetadata = new ClassMetadata
            {
                SourceAssemblyName = t.Module.Name,
                SourceAssemblyPath = t.Module.FileName,
                SourceClass = t.DeclaringType?.Name, // TODO: Is this needed?
                SourceNamespace = t.Namespace,
                ClassName = typeName
            };
            Classes.TryAdd(typeName, classMetadata);
        });
    }

    internal void AddMethods(IEnumerable<MethodDefinition> methods)
    {
        methods.ToList().ForEach(m =>
        {
            var signature = m.FullName;
            if (Methods.ContainsKey(signature))
            {
                return;
            }

            var methodMetadata = new MethodMetadata
            {
                SourceAssemblyName = m.Module.Name,
                SourceAssemblyPath = m.Module.FileName,
                SourceClass = m.DeclaringType.Name,
                SourceNamespace = m.DeclaringType.Namespace,
                Signature = signature
            };
            Methods.TryAdd(signature, methodMetadata);
        });
    }
}