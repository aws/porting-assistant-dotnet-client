namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility.MetadataModels;

public class ILInstructionMetadata
{
    public string SourceAssemblyName { get; set; }
    public string SourceAssemblyPath { get; set; }
    public string SourceClass { get; set; }
    public string SourceNamespace { get; set; }
    public string Signature { get; set; }
    public string ILSignature { get; set; }

    public MethodMetadata ToMethodMetadata()
    {
        throw new NotImplementedException();
    }
}