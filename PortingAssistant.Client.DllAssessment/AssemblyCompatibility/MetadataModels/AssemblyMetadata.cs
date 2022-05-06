using PortingAssistant.Client.DllAssessment.AssemblyCompatibility.TargetFramework;

namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility.MetadataModels;

public enum AssemblyType
{
    Sdk,
    Nuget
}

public abstract class AssemblyMetadata
{
    public abstract AssemblyType AssemblyType { get; }
    public string AssemblyPath { get; set; }
    public TargetFrameworkMoniker TargetFramework { get; set; }
    public bool IsNetCoreCompatible { get; set; }
    public bool IsWindowsOnly { get; set; }
    public Dictionary<string, ClassMetadata> Classes { get; set; } = new();
    public Dictionary<string, MethodMetadata> Methods { get; set; } = new();
}