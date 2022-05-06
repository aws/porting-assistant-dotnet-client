namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility.MetadataModels
{
    internal class MethodCompatibility : BaseCompatibility
    {
        internal MethodCompatibility()
        {
        }

        internal MethodCompatibility(AssemblyCompatibility assemblyCompatibility)
        {
            IsNetCoreCompatible = assemblyCompatibility.IsNetCoreCompatible;
            IsLinuxCompatible = assemblyCompatibility.IsLinuxCompatible;
            IsWindowsCompatibleOnly = assemblyCompatibility.IsWindowsCompatibleOnly;
        }

        internal static MethodCompatibility GetIncompatibleInstance()
        {
            return new MethodCompatibility
            {
                IsNetCoreCompatible = false,
                IsLinuxCompatible = false,
                IsWindowsCompatibleOnly = true
            };
        }
    }
}