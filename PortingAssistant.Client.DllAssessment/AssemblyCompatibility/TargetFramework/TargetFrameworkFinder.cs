using Mono.Cecil;
using System.Runtime.Versioning;

namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility.TargetFramework;

public class TargetFrameworkFinder
{
    private static readonly IDictionary<string, TargetFrameworkMoniker> AttributeToTargetFrameworkMap = new Dictionary<string, TargetFrameworkMoniker>
    {
        // TODO: Map values for .NET 4.0 and above
        // .NET Portable is a special case as it is a legacy moniker.
        // Mappings for .NET Portable targets are best guess based on TargetFrameworks specified on Nuget packages
        { ".NETPortable,Version=v5.0", TargetFrameworkMoniker.NetStandard10 },

        { ".NETStandard,Version=v1.0", TargetFrameworkMoniker.NetStandard10 },
        { ".NETStandard,Version=v1.1", TargetFrameworkMoniker.NetStandard11 },
        { ".NETStandard,Version=v1.2", TargetFrameworkMoniker.NetStandard12 },
        { ".NETStandard,Version=v1.3", TargetFrameworkMoniker.NetStandard13 },
        { ".NETStandard,Version=v1.4", TargetFrameworkMoniker.NetStandard14 },
        { ".NETStandard,Version=v1.5", TargetFrameworkMoniker.NetStandard15 },
        { ".NETStandard,Version=v1.6", TargetFrameworkMoniker.NetStandard16 },
        { ".NETStandard,Version=v2.0", TargetFrameworkMoniker.NetStandard20 },
        { ".NETStandard,Version=v2.1", TargetFrameworkMoniker.NetStandard21 },
        { ".NETCoreApp,Version=v1.0", TargetFrameworkMoniker.NetCoreApp10 },
        { ".NETCoreApp,Version=v1.1", TargetFrameworkMoniker.NetCoreApp11 },
        { ".NETCoreApp,Version=v2.0", TargetFrameworkMoniker.NetCoreApp20 },
        { ".NETCoreApp,Version=v2.1", TargetFrameworkMoniker.NetCoreApp21 },
        { ".NETCoreApp,Version=v2.2", TargetFrameworkMoniker.NetCoreApp22 },
        { ".NETCoreApp,Version=v3.0", TargetFrameworkMoniker.NetCoreApp30 },
        { ".NETCoreApp,Version=v3.1", TargetFrameworkMoniker.NetCoreApp31 },
        { ".NETCoreApp,Version=v5.0", TargetFrameworkMoniker.Dotnet5 },
        { ".NETCoreApp,Version=v6.0", TargetFrameworkMoniker.Dotnet6 }
    };

    // TODO: update return type to an enum or some other target framework object
    public static TargetFrameworkMoniker GetTargetFramework(ModuleDefinition moduleDefinition)
    {
        try
        {
            // TargetFrameworkAttribute is only defined for .NET Framework 4.0 and above
            var targetFrameworkAttribute = GetTargetFrameworkAttribute(moduleDefinition);
            if (targetFrameworkAttribute is null)
            {
                Console.WriteLine($"TargetFrameworkAttribute for {moduleDefinition.Assembly} could not be found. This assembly is most likely targeting .NET 2, 3, or 3.5.");
                return TargetFrameworkMoniker.Unknown;
            }

            var targetFramework = GetTargetFrameworkFromAttribute(targetFrameworkAttribute);
            if (targetFramework != TargetFrameworkMoniker.Unknown)
            {
                return targetFramework;
            }

            Console.WriteLine($"Could not parse TargetFramework from TargetFrameworkAttribute {targetFrameworkAttribute}, {string.Join(", ", targetFrameworkAttribute.ConstructorArguments)}.");
            return TargetFrameworkMoniker.Unknown;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return TargetFrameworkMoniker.Unknown;
        }
    }

    private static TargetFrameworkMoniker GetTargetFrameworkFromAttribute(ICustomAttribute? targetFrameworkAttribute)
    {
        var constructorArgument = targetFrameworkAttribute?
            .ConstructorArguments
            .Select(arg => arg.Value.ToString() ?? string.Empty)
            .FirstOrDefault(arg => AttributeToTargetFrameworkMap.ContainsKey(arg));

        return string.IsNullOrEmpty(constructorArgument)
            ? TargetFrameworkMoniker.Unknown
            : AttributeToTargetFrameworkMap[constructorArgument];
    }

    private static CustomAttribute? GetTargetFrameworkAttribute(ModuleDefinition moduleDefinition)
    {
        return moduleDefinition
            .GetCustomAttributes()
            .FirstOrDefault(a => a.AttributeType.Name.Equals(nameof(TargetFrameworkAttribute)));
    }
}