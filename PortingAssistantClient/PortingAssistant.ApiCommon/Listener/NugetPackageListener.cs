using PortingAssistantApiCommon.Model;
using PortingAssistantCommon.Model;

namespace PortingAssistantApiCommon.Listener
{
    public delegate void OnNugetPackageUpdate(Response<PackageVersionResult, PackageVersionPair> response);
}
