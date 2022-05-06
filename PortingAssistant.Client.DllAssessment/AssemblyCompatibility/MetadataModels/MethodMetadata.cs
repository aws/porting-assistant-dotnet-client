namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility.MetadataModels;

public enum AccessModifier
{
    Public,
    Private,
    Protected,
    PrivateProtected
}

public class MethodMetadata
{
    public string SourceAssemblyName { get; set; }
    public string SourceAssemblyPath { get; set; }
    public string SourceClass { get; set; }
    public string SourceNamespace { get; set; }
    public string Signature { get; set; }
    public AccessModifier AccessModifier { get; set; }
    public bool IsNetCoreCompatible { get; set; }
    public bool IsLinuxCompatible { get; set; }
    public bool IsWindowsOnly { get; set; }
}