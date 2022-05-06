namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility.MetadataModels;

public class ClassMetadata
{
    public string SourceAssemblyName { get; set; }
    public string SourceAssemblyPath { get; set; }
    public string SourceClass { get; set; }
    public string SourceNamespace { get; set; }
    public string ClassName { get; set; }
    public Dictionary<string, MethodMetadata> Methods { get; set; } = new ();
}