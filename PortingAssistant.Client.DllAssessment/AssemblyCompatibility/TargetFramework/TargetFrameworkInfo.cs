namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility.TargetFramework;

public class TargetFrameworkInfo
{
    public readonly TargetFrameworkMoniker TargetFrameworkMoniker;
    public readonly TargetFrameworkMonikerType TargetFrameworkMonikerType;
    public readonly TargetFrameworkType TargetFrameworkType;

    public TargetFrameworkInfo(
        TargetFrameworkMoniker targetFrameworkMoniker,
        TargetFrameworkMonikerType targetFrameworkMonikerType,
        TargetFrameworkType targetFrameworkType)
    {
        TargetFrameworkMoniker = targetFrameworkMoniker;
        TargetFrameworkMonikerType = targetFrameworkMonikerType;
        TargetFrameworkType = targetFrameworkType;
    }

    public override string ToString()
    {
        return TargetFrameworkMoniker.Value;
    }
}
