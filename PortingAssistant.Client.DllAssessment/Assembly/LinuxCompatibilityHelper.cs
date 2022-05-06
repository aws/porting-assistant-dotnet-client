using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AwsEncoreService.Compatibility.Handler;
using AwsEncoreService.Compatibility.Model;
using NuGet.Packaging;

namespace AwsEncoreServiceCache.Compatibility.Handler.Assembly
{
    public class LinuxCompatibilityHelper
    {
        private const string SupportedOSPlatformAttribute = "SupportedOSPlatformAttribute";
        private const string LinuxOsPlatform = "linux";

        /// <summary>
        /// Determines if an assembly has been declared linux compatible at the assembly level.
        /// </summary>
        /// <param name="dllPath">Path of the assembly file</param>
        /// <returns>If the assembly is compatible with linux</returns>
        public static bool IsAssemblySupportedOnLinux(string dllPath)
        {
            try
            {
                // Loading the assembly with its bytes removes the file handle after bytes have 
                // been loaded into memory.
                var assembly = System.Reflection.Assembly.Load(File.ReadAllBytes(dllPath));

                // Check if this assembly has an inclusive list of supported OS platforms
                // Note: this inclusion list would be defined in the .csproj or a Build.props file
                // Example: 
                //    <ItemGroup>
                //      <SupportedPlatform Include="windows" />
                //      <SupportedPlatform Include="linux" />
                //    </ItemGroup >
                var supportedOsInclusionList = assembly.CustomAttributes.Where(attr =>
                        attr.AttributeType.Name == SupportedOSPlatformAttribute)
                    .SelectMany(attr => attr.ConstructorArguments)
                    .Select(arg => arg.Value?.ToString() ?? string.Empty)
                    .ToList();

                // If there's no inclusion list, then linux is supported
                if (!supportedOsInclusionList.Any())
                    return true;

                // If the inclusion list exists and linux is in it, then linux is supported
                return supportedOsInclusionList.Any(os =>
                    string.Equals(os, LinuxOsPlatform, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not analyze {dllPath} for linux compatibility: {e}");
                return true;
            }
        }

        /// <summary>
        /// Use UnsupportedApiFinder to identify all linux-incompatible apis in an assembly.
        /// </summary>
        /// <param name="assemblyModel">Assembly to search for unsupported apis</param>
        /// <returns>A set of method signatures for incompatible apis</returns>
        public static ISet<string> GetUnsupportedMethodsInAssembly(DotnetAssemblyModel assemblyModel)
        {
            var apiFinder = new UnsupportedApiFinder(assemblyModel.DllPath);
            try
            {
                apiFinder.ComputeUnsupportedApis();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error encountered during API compatibility analysis for {assemblyModel.DllName} {assemblyModel.DllVersion}: {e}");
                Console.WriteLine("Continuing with api compatibility analysis.");
            }

            var unsupportedApis = new HashSet<string>();
            var list = new List<string>();
            var missing = 0;
            foreach (var methodInfo in assemblyModel.Methods)
            {
                if (apiFinder.IsUnsupportedApi(methodInfo.signature))
                {
                    list.Add(methodInfo.signature);
                }

                if (!apiFinder.IsApiDefined(methodInfo.signature))
                {
                    Console.WriteLine("WARN: Missing Api: " + methodInfo.signature);
                    missing++;
                }
            }

            if (list.Count > 0)
            {
                Console.WriteLine("Platform Unsupported methods on Linux: ");
                Console.WriteLine(string.Join("\n", list));
                unsupportedApis.AddRange(list);
            }

            var summary =
                $"Api Summary: Dll {assemblyModel.DllName}, {assemblyModel.Methods.Count} Methods, Unsupported {list.Count}, Missing {missing}";
            Console.WriteLine(summary);

            return unsupportedApis;
        }
    }
}
