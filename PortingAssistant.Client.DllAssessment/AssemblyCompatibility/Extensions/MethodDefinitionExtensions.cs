using Mono.Cecil;

namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility.Extensions;

public static class MethodDefinitionExtensions
{
    public static bool IsLinuxCompatible(this MethodDefinition method)
    {
        const string supportedOsPlatformAttribute = "SupportedOSPlatformAttribute";
        const string unsupportedOsPlatformAttribute = "UnsupportedOSPlatformAttribute";
        const string linuxOsPlatform = "linux";
        if (!method.HasCustomAttributes)
        {
            return true;
        }

        var attrs = method.CustomAttributes;
        return attrs.Any(a =>
        {
            // If there is a platform inclusion list and linux is not included, return false
            if (a.AttributeType.Name == supportedOsPlatformAttribute
                && a.ConstructorArguments.All(arg => arg.Value?.ToString() != linuxOsPlatform))
            {
                return false;
            }

            // If there is a platform exclusion list and linux is included, return false
            if (a.AttributeType.Name == unsupportedOsPlatformAttribute
                && a.ConstructorArguments.Any(arg => arg.Value?.ToString() == linuxOsPlatform))
            {
                return false;
            }

            // By default, return true
            return true;
        });
    }
}