using System;
using System.Linq;
using EncoreCommon.Model;
using Semver;

namespace EncoreApiAnalysis.Utils
{
    public static class ApiCompatiblity
    {
        private const string DEFAULT_TARGET = "netcoreapp3.1";

        public static bool apiInPackageVersion(PackageDetails package, string apiMethodSignature, string version, string target = DEFAULT_TARGET, bool checkLesserPackage = true)
        {
            if (package == null || apiMethodSignature == null)
            {
                return false;
            }

            var foundApi = GetApiDetails(package, apiMethodSignature);

            if(foundApi == null)
            {
                if(!checkLesserPackage || package.Targets == null || !package.Targets.TryGetValue(target, out var targetFramework))
                {
                    return false;
                }

                return hasLesserTarget(version, targetFramework.ToArray());
            }

            if(! foundApi.Targets.TryGetValue(target, out var framework)) {
                return false;
            }

            return hasLesserTarget(version, framework.ToArray());

        }

        private static bool hasLesserTarget(string version, string[] targetVersions)
        {
            if(!SemVersion.TryParse(version, out var target))
            {
                return false;
            }

            return targetVersions.Any(v => SemVersion.Compare(target, SemVersion.Parse(v)) > 0);
        }

        public static string upgradeStrategy(PackageDetails nugetPackage, string apiMethodSignature, string version)
        {
            if(nugetPackage == null || apiMethodSignature == null || version == null)
            {
                return null;
            }

            var targetApi = GetApiDetails(nugetPackage, apiMethodSignature);
            if(targetApi == null || targetApi.Targets == null || !targetApi.Targets.TryGetValue(DEFAULT_TARGET, out var versions))
            {
                return null;
            }

            var upgradeVersion = versions.Last();
            try
            {
                if(SemVersion.Compare(SemVersion.Parse(version), SemVersion.Parse(upgradeVersion)) > 0)
                {
                    return null;
                }
                return upgradeVersion;
            } catch
            {
                return null;
            }
        }

        private static ApiDetails GetApiDetails(PackageDetails nugetPackage, string apiMethodSignature)
        {
            var foundApi = nugetPackage.Api.FirstOrDefault(api => api.MethodSignature == apiMethodSignature);
            if (foundApi == null)
            {
                foundApi = nugetPackage.Api.FirstOrDefault(api =>
                {
                    if (
                    api.MethodParameters == null ||
                    api.MethodParameters.Length == 0 ||
                    api.MethodSignature == null ||
                    api.MethodName == null
                    )
                    {
                        return false;
                    }

                    var possibleExtension = api.MethodParameters[0];
                    var sliceMethodSignature = api.MethodSignature.Substring(0, api.MethodSignature.IndexOf("("));
                    var methodName = sliceMethodSignature.Substring(sliceMethodSignature.LastIndexOf(api.MethodName));
                    var methodSignature = $"${possibleExtension}.${methodName}(${String.Join(",", api.MethodParameters.Take(1))}";
                    return methodSignature == apiMethodSignature.Replace("?", "");
                });
            }

            return foundApi;
        }
    }
}
