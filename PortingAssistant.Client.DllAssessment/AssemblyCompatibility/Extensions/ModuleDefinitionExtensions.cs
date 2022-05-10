using Mono.Cecil;
using PortingAssistant.Client.DllAssessment.AssemblyCompatibility.TargetFramework;

namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility.Extensions;

internal static class ModuleDefinitionExtensions
{
    public static ISet<ModuleDefinition> GetReferencedAssemblies(this ModuleDefinition assembly, DefaultAssemblyResolver assemblyResolver)
    {
        if (!assembly.HasAssemblyReferences)
        {
            return new HashSet<ModuleDefinition>();
        }

        var resolvedAssemblyReferences = assembly.AssemblyReferences.SelectMany(assemblyReference =>
        {
            try
            {
                return assemblyResolver.Resolve(assemblyReference).Modules.ToHashSet();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new HashSet<ModuleDefinition>();
            }
        }).ToHashSet();
        return resolvedAssemblyReferences;
    }

    public static bool? IsNetCoreCompatible(this ModuleDefinition moduleDefinition)
    {
        var targetFramework = TargetFrameworkFinder.GetTargetFramework(moduleDefinition);

        if (targetFramework.Equals(TargetFrameworkMoniker.Unknown))
        {
            return null;
        }

        return targetFramework >= TargetFrameworkMoniker.NetStandard10;
    }

    public static bool? IsLinuxCompatible(
        this ModuleDefinition moduleDefinition, 
        ISet<ModuleDefinition>? incompatibleMicrosoftDllNames = null, 
        bool? isNetCoreCompatible = null)
    {
        const string supportedOsPlatformAttribute = "SupportedOSPlatformAttribute";
        const string linuxOsPlatform = "linux";

        try
        {
            isNetCoreCompatible ??= moduleDefinition.IsNetCoreCompatible();
            incompatibleMicrosoftDllNames ??= new HashSet<ModuleDefinition>();
            
            if (isNetCoreCompatible is false or null)
            {
                return isNetCoreCompatible;
            }

            var assemblyFileName = Path.GetFileName(method.Module.FileName);
            if (incompatibleMicrosoftDllNames.Contains(assemblyFileName))
            {
                return true;
            }

            // Check if this assembly has an inclusive list of supported OS platforms
            // Note: this inclusion list would be defined in the .csproj or a Build.props file
            // Example: 
            //    <ItemGroup>
            //      <SupportedPlatform Include="windows" />
            //      <SupportedPlatform Include="linux" />
            //    </ItemGroup >
            var supportedOsInclusionList = moduleDefinition.Assembly.CustomAttributes.Where(attr =>
                    attr.AttributeType.Name == supportedOsPlatformAttribute)
                .SelectMany(attr => attr.ConstructorArguments)
                .Select(arg => arg.Value?.ToString() ?? string.Empty)
                .ToList();

            // If there's no inclusion list, then linux is supported
            if (!supportedOsInclusionList.Any())
                return true;

            // If the inclusion list exists and linux is in it, then linux is supported
            return supportedOsInclusionList.Any(os =>
                string.Equals(os, linuxOsPlatform, StringComparison.InvariantCultureIgnoreCase));
        }
        catch (Exception e)
        {
            Console.WriteLine($"Could not analyze assembly {moduleDefinition.Assembly} for linux compatibility: {e}");
            return true;
        }
        
    }

    public static bool IsFromMicrosoft(this ModuleDefinition assembly)
    {
        return assembly.FileName.Contains(Path.Combine("dotnet", "sdk"))
               || assembly.FileName.Contains(Path.Combine("dotnet", "shared"));
    }
}