namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility.TargetFramework;

/// <summary>
/// Types of target framework monikers used in .csproj files
/// </summary>
public enum TargetFrameworkMonikerType
{
    Net,
    NetStandard,
    NetCoreApp,
    Dotnet
}

/// <summary>
/// Target framework type of target framework monikers used in .csproj files
/// </summary>
public enum TargetFrameworkType
{
    DotnetFramework,
    DotnetCore
}

/// <summary>
/// An enum-like class of target framework monikers used in .csproj files
/// </summary>
public readonly struct TargetFrameworkMoniker : IComparable<TargetFrameworkMoniker>, IEquatable<TargetFrameworkMoniker>
{
    public readonly string Value;
    public readonly int EnumValue;

    private TargetFrameworkMoniker(string value, int enumValue)
    {
        Value = value;
        EnumValue = enumValue;
    }

    // Define enum-like values
    public static TargetFrameworkMoniker Unknown => new ("unknown", -1);
    public static TargetFrameworkMoniker NetFramework11 => new ("net11", 0);
    public static TargetFrameworkMoniker NetFramework20 => new ("net20", 1);
    public static TargetFrameworkMoniker NetFramework35 => new ("net35", 2);
    public static TargetFrameworkMoniker NetFramework40 => new ("net40", 3);
    public static TargetFrameworkMoniker NetFramework403 => new ("net403", 4);
    public static TargetFrameworkMoniker NetFramework45 => new ("net45", 5);
    public static TargetFrameworkMoniker NetFramework451 => new ("net451", 6);
    public static TargetFrameworkMoniker NetFramework452 => new ("net452", 7);
    public static TargetFrameworkMoniker NetFramework46 => new ("net46", 9);
    public static TargetFrameworkMoniker NetFramework461 => new ("net461", 10);
    public static TargetFrameworkMoniker NetFramework462 => new ("net462", 11);
    public static TargetFrameworkMoniker NetFramework47 => new ("net47", 12);
    public static TargetFrameworkMoniker NetFramework471 => new ("net471", 13);
    public static TargetFrameworkMoniker NetFramework472 => new ("net472", 14);
    public static TargetFrameworkMoniker NetFramework48 => new ("net48", 15);
    public static TargetFrameworkMoniker NetStandard10 => new ("netstandard1.0", 16);
    public static TargetFrameworkMoniker NetStandard11 => new ("netstandard1.1", 17);
    public static TargetFrameworkMoniker NetStandard12 => new ("netstandard1.2", 18);
    public static TargetFrameworkMoniker NetStandard13 => new ("netstandard1.3", 19);
    public static TargetFrameworkMoniker NetStandard14 => new ("netstandard1.4", 20);
    public static TargetFrameworkMoniker NetStandard15 => new ("netstandard1.5", 21);
    public static TargetFrameworkMoniker NetStandard16 => new ("netstandard1.6", 22);
    public static TargetFrameworkMoniker NetStandard20 => new ("netstandard2.0", 23);
    public static TargetFrameworkMoniker NetStandard21 => new ("netstandard2.1", 24);
    public static TargetFrameworkMoniker NetCoreApp10 => new ("netcoreapp1.0", 25);
    public static TargetFrameworkMoniker NetCoreApp11 => new ("netcoreapp1.1", 26);
    public static TargetFrameworkMoniker NetCoreApp20 => new ("netcoreapp2.0", 27);
    public static TargetFrameworkMoniker NetCoreApp21 => new ("netcoreapp2.1", 28);
    public static TargetFrameworkMoniker NetCoreApp22 => new ("netcoreapp2.2", 29);
    public static TargetFrameworkMoniker NetCoreApp30 => new ("netcoreapp3.0", 30);
    public static TargetFrameworkMoniker NetCoreApp31 => new ("netcoreapp3.1", 31);
    public static TargetFrameworkMoniker Dotnet5 => new ("net5.0", 32);
    public static TargetFrameworkMoniker Dotnet6 => new ("net6.0", 33);
    public static TargetFrameworkMoniker Dotnet7 => new ("net7.0", 34);

    // Define comparison methods and operators
    public int CompareTo(TargetFrameworkMoniker toCompare)
    {
        return EnumValue.CompareTo(toCompare.EnumValue);
    }

    public override bool Equals(object toCompare)
    {
        return toCompare is TargetFrameworkMoniker other && Equals(other);
    }

    public bool Equals(TargetFrameworkMoniker other)
    {
        return Value == other.Value && EnumValue == other.EnumValue;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, EnumValue);
    }

    public static bool operator ==(TargetFrameworkMoniker tfm1, TargetFrameworkMoniker tfm2)
    {
        return tfm1.Value == tfm2.Value;
    }

    public static bool operator !=(TargetFrameworkMoniker tfm1, TargetFrameworkMoniker tfm2)
    {
        return tfm1.Value != tfm2.Value;
    }

    public static bool operator <(TargetFrameworkMoniker tfm1, TargetFrameworkMoniker tfm2)
    {
        return tfm1.EnumValue < tfm2.EnumValue;
    }

    public static bool operator >(TargetFrameworkMoniker tfm1, TargetFrameworkMoniker tfm2)
    {
        return tfm1.EnumValue > tfm2.EnumValue;
    }

    public static bool operator <=(TargetFrameworkMoniker tfm1, TargetFrameworkMoniker tfm2)
    {
        return tfm1.EnumValue < tfm2.EnumValue || tfm1.EnumValue == tfm2.EnumValue;
    }

    public static bool operator >=(TargetFrameworkMoniker tfm1, TargetFrameworkMoniker tfm2)
    {
        return tfm1.EnumValue > tfm2.EnumValue || tfm1.EnumValue == tfm2.EnumValue;
    }
}

public class TargetFrameworks
{
    /// <summary>
    /// A lookup that maps a target framework moniker to a TargetFrameworkInfo object, a data structure containing metadata about the target framework.
    /// </summary>
    public static readonly Dictionary<string, TargetFrameworkInfo> Lookup = new Dictionary<string, TargetFrameworkInfo>
        {
            {TargetFrameworkMoniker.NetFramework11.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework11, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework20.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework20, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework35.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework35, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework40.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework40, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework403.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework403, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework45.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework45, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework451.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework451, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework452.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework452, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework46.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework46, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework461.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework461, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework462.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework462, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework47.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework47, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework471.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework471, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework472.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework472, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetFramework48.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetFramework48, TargetFrameworkMonikerType.Net, TargetFrameworkType.DotnetFramework)},
            {TargetFrameworkMoniker.NetStandard10.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetStandard10, TargetFrameworkMonikerType.NetStandard, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetStandard11.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetStandard11, TargetFrameworkMonikerType.NetStandard, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetStandard12.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetStandard12, TargetFrameworkMonikerType.NetStandard, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetStandard13.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetStandard13, TargetFrameworkMonikerType.NetStandard, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetStandard14.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetStandard14, TargetFrameworkMonikerType.NetStandard, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetStandard15.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetStandard15, TargetFrameworkMonikerType.NetStandard, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetStandard16.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetStandard16, TargetFrameworkMonikerType.NetStandard, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetStandard20.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetStandard20, TargetFrameworkMonikerType.NetStandard, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetStandard21.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetStandard21, TargetFrameworkMonikerType.NetStandard, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetCoreApp10.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetCoreApp10, TargetFrameworkMonikerType.NetCoreApp, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetCoreApp11.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetCoreApp11, TargetFrameworkMonikerType.NetCoreApp, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetCoreApp20.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetCoreApp20, TargetFrameworkMonikerType.NetCoreApp, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetCoreApp21.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetCoreApp21, TargetFrameworkMonikerType.NetCoreApp, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetCoreApp22.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetCoreApp22, TargetFrameworkMonikerType.NetCoreApp, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetCoreApp30.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetCoreApp30, TargetFrameworkMonikerType.NetCoreApp, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.NetCoreApp31.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.NetCoreApp31, TargetFrameworkMonikerType.NetCoreApp, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.Dotnet5.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.Dotnet5, TargetFrameworkMonikerType.Dotnet, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.Dotnet6.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.Dotnet6, TargetFrameworkMonikerType.Dotnet, TargetFrameworkType.DotnetCore)},
            {TargetFrameworkMoniker.Dotnet7.Value, new TargetFrameworkInfo(TargetFrameworkMoniker.Dotnet7, TargetFrameworkMonikerType.Dotnet, TargetFrameworkType.DotnetCore)},
        };

    public static bool IsDotnetCoreCompatible(TargetFrameworkMoniker targetFrameworkMoniker)
    {
        return IsDotnetCoreCompatible(targetFrameworkMoniker.Value);
    }

    public static bool IsDotnetCoreCompatible(string targetFramework)
    {
        if (Lookup.TryGetValue(targetFramework, out var targetFrameworkInfo))
        {
            return targetFrameworkInfo.TargetFrameworkType == TargetFrameworkType.DotnetCore;
        }

        return false;
    }

    public static bool AreSupportedPlatformAttributesDetectable(string targetFramework)
    {
        // Starting with .NET5, SupportedOSPlatformAttribute and UnsupportedOSPlatformAttribute are public and detectable.
        // In prior versions, these attributes were internal and undetectable
        var targetFrameworkInfo = Lookup[targetFramework];
        return targetFrameworkInfo.TargetFrameworkMoniker >= TargetFrameworkMoniker.Dotnet5;
    }
}
