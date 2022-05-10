using PortingAssistant.Client.DllAssessment.AssemblyCompatibility;


namespace PortingAssistant.Client.DllAssessment
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var dllPath = @"D:\dll_assessment\output_net6\new\MvcExample_net6.dll";
            var dllDir = Path.GetDirectoryName(dllPath) ?? string.Empty;
            var sdkAndRuntimePaths = new List<string>
            {
                dllDir, // Nuget packages
                @"C:\Program Files\dotnet\sdk\6.0.201", // SDK dlls
                @"C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\6.0.3", // Nuget packages
                @"C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All\2.1.30",
                @"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\6.0.3",
                //@"C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\6.0.3", // Windows only
            };

            // [x] TODO: Cache dlls from "Microsoft.WindowsDesktop.App" as incompatible from MS
            // [x] TODO: Add previous logic to check for "throws PlatformNotSupported"
            // TODO: Convert incompatible IL api names to c# names
            // TODO: Ensure constructors and property names are accounted for
            // TODO: Unit tests
            // TODO: Add SerializeWithCOmpatibility(Compatibility) extension methods to convert ModuleDefinion and MethodDefinion to POCO objects, then write them to json
            // TODO: Issue: Semantic information is lost (e.g. System.DirectoryServices is incompatible assembly. Trying to call DirectoryEntry.ToString() results in System.Object.ToString() being detected.)
            // TODO: Need to report incompatible assemblies used and add test case using specific incompatible APIs (System.Console.CapsLock property, Console.Beep())
            // TODO: Resolve edge cases where we are not able to detect target framework version (can we use Metadata Attribute and look at TargetFramework constructor arg?)
            // TODO: Try finding all dlls that are entry point dlls. Will this cause issues?
            var cataloger = new CompatibilityCataloger(dllPath, sdkAndRuntimePaths);
            cataloger.Assess();
            
            var incompatibleApisUsedInProject = cataloger.IncompatibleMethodsReferencedInProject;
            var incompatibleAssemblies = cataloger.IncompatibleAssemblies;
            var incompatibleApis = cataloger.IncompatibleMethods;
            var unknownAssemblies = cataloger.AssembliesWithUnknownCompatibility;
            var unknownMethods = cataloger.MethodsWithUnknownCompatibility;

            //File.WriteAllText($"{nameof(incompatibleAssemblies)}.json", JsonConvert.SerializeObject(incompatibleAssemblies, Formatting.Indented));
            //File.WriteAllText($"{nameof(incompatibleApis)}.json", JsonConvert.SerializeObject(incompatibleApis, Formatting.Indented));
            //File.WriteAllText($"{nameof(unknownAssemblies)}.json", JsonConvert.SerializeObject(unknownAssemblies, Formatting.Indented));
            //File.WriteAllText($"{nameof(unknownMethods)}.json", JsonConvert.SerializeObject(unknownMethods, Formatting.Indented));
            //File.WriteAllText($"{nameof(incompatibleApisUsedInProject)}.json", JsonConvert.SerializeObject(incompatibleApisUsedInProject, Formatting.Indented));

            Console.WriteLine($"{incompatibleAssemblies.Count} incompatible assemblies found.");
            Console.WriteLine($"{incompatibleApis.Count} incompatible apis found.");
            Console.WriteLine($"{unknownAssemblies.Count} assemblies with unknown compatibility found.");
            Console.WriteLine($"{unknownMethods.Count} methods with unknown compatibility found.");
            Console.WriteLine($"{incompatibleApisUsedInProject.Count()} incompatible apis used in solution.");

            Console.ReadLine();
            Console.WriteLine();
            
            var exePath = @"D:\dll_assessment\output_net6\MvcExample_net6.exe";

            //var moduleReferences = sdkAndRuntimePaths
            //    .SelectMany(p => Directory.EnumerateFiles(p, ".dll"))
            //    .Select(dll => new PEFile(dll));

            //var compilation = new SimpleCompilation(new PEFile(dllPath), moduleReferences);
            //    var context = new CSharpTypeResolveContext();
            //var resolver = new CSharpResolver(compilation);

            //var universal = new UniversalAssemblyResolver(
            //    dllPath, 
            //    true,
            //    ".NETCOREAPP");
            //foreach (var directory in  sdkAndRuntimePaths)
            //{
            //    universal.AddSearchDirectory(directory);
            //}

            //var wholeProjectDecompiler = new WholeProjectDecompiler(universal);
            //wholeProjectDecompiler.DecompileProject();

            // Simple decompilation
            //var decompilerSettings = new DecompilerSettings(LanguageVersion.CSharp10_0);
            //var decompiler = new CSharpDecompiler(dllPath, decompilerSettings);
            //decompiler.Decompile();

            //var assembliesToAnalyze = new List<string>
            //{
            //    dllPath
            //};
            //cataloger.CatalogAssemblies(assembliesToAnalyze);
            //cataloger.CatalogAssembly(dllPath);
        }
    }
}