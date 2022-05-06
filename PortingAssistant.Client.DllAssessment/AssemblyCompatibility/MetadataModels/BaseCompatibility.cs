namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility.MetadataModels
{
    internal abstract class BaseCompatibility
    {
        public BaseCompatibility() {}

        // Null compatibility values means the compatibility is unknown
        public bool? IsNetCoreCompatible { get; set; } = null;
        public bool? IsLinuxCompatible { get; set; } = null;
        public bool? IsWindowsCompatibleOnly { get; set; } = null;
    }
}