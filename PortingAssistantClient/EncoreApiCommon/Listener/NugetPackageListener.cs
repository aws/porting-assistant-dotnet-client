using EncoreApiCommon.Model;
using EncoreCommon.Model;

namespace EncoreApiCommon.Listener
{
    public delegate void OnNugetPackageUpdate(Response<PackageVersionResult, PackageVersionPair> response);
}
