using Amazon.DynamoDBv2.Model;
using Mono.Cecil;
using Mono.Cecil.Cil;
using PortingAssistant.Client.DllAssessment.AssemblyCompatibility.Extensions;
using PortingAssistant.Client.DllAssessment.AssemblyCompatibility.MetadataModels;

namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility;

public class CompatibilityCataloger
{
    private const string Reference = "?";

    private readonly string _assemblyFile;
    private readonly ModuleDefinition _moduleDefinition;
    private readonly ISet<string> _assembliesFromMicrosoft;
    private readonly DefaultAssemblyResolver _assemblyResolver;
    private readonly ReaderParameters _readerParameters;

    // Public unsupported methods
    private ISet<string> _publicUnsupportedMethods;
    private int _unsupportedMethodsCount = 0;
    private readonly bool _isDebug = false;
    private readonly ISet<string> UNSUPPORTED_EXCEPTIONS;
    private readonly Dictionary<string, string> _methodsIL2CSharpMap;

    //public AssemblyCompatibilityCatalog Catalog { get; } = new();

    internal Dictionary<ModuleDefinition, MetadataModels.AssemblyCompatibility> AssemblyCompatibilities { get; } = new();

    internal Dictionary<MethodDefinition, MethodCompatibility> MethodCompatibilities { get; } = new();

    public ISet<ModuleDefinition> IncompatibleAssemblies
    {
        get
        {
            var incompatibleAssemblies = AssemblyCompatibilities.Keys
                .Where(assembly => AssemblyCompatibilities[assembly].IsLinuxCompatible == false);

            return incompatibleAssemblies.ToHashSet();
        }
    }

    public ISet<MethodDefinition> IncompatibleMethods
    {
        get
        {
            var incompatibleMethods = MethodCompatibilities.Keys
                .Where(method => MethodCompatibilities[method].IsLinuxCompatible == false);

            return incompatibleMethods.ToHashSet();
        }
    }

    public ISet<ModuleDefinition> AssembliesWithUnknownCompatibility
    {
        get
        {
            var assembliesWithUnknownCompatibility = AssemblyCompatibilities.Keys
                .Where(assembly => AssemblyCompatibilities[assembly].IsNetCoreCompatible == null);

            return assembliesWithUnknownCompatibility.ToHashSet();
        }
    }

    public ISet<MethodDefinition> MethodsWithUnknownCompatibility
    {
        get
        {
            var methodsWithUnknownCompatibility = MethodCompatibilities.Keys
                .Where(method => MethodCompatibilities[method].IsNetCoreCompatible == null);

            return methodsWithUnknownCompatibility.ToHashSet();
        }
    }

    public CompatibilityCataloger(string assemblyFile, IEnumerable<string>? searchDirectories)
    {
        // Initialize the resolver
        _assemblyResolver = new DefaultAssemblyResolver();

        // Enable resolution of any other assemblies
        searchDirectories ??= new List<string>();
        foreach (var directory in searchDirectories)
        {
            _assemblyResolver.AddSearchDirectory(directory);
        }

        _readerParameters = new ReaderParameters
        {
            AssemblyResolver = _assemblyResolver
        };

        _assemblyFile = assemblyFile;
        _moduleDefinition = ModuleDefinition.ReadModule(_assemblyFile, _readerParameters);
        _assembliesFromMicrosoft = GetAssembliesFromMicrosoft(searchDirectories);

        //
        UNSUPPORTED_EXCEPTIONS = new HashSet<string>
        {
            "System.Void System.PlatformNotSupportedException::.ctor()",
            "System.Void System.NotSupportedException::.ctor(System.String)"
        };

        _methodsIL2CSharpMap = new Dictionary<string, string>();
        _publicUnsupportedMethods = new HashSet<string>();
    }

    private ISet<string> GetAssembliesFromMicrosoft(IEnumerable<string> searchDirectories)
    {
        Console.WriteLine("Searching for assemblies written by Microsoft...");
        var dotnetDirectories = searchDirectories.Where(d =>
            d.Contains(Path.Combine("dotnet", "sdk"))
            || d.Contains(Path.Combine("dotnet", "shared")));

        var dlls = dotnetDirectories.SelectMany(d => Directory.EnumerateFiles(d, "*.dll"))
            .ToHashSet()
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .ToHashSet();

        Console.WriteLine($"Found {dlls.Count} assemblies.");
        return dlls;
    }

    public IEnumerable<MethodDefinition> Assess()
    {
        Console.WriteLine("Building dependency graph...");
        var dependencyGraph = BuildDependencyGraph(_moduleDefinition);
        if (dependencyGraph.Any())
        {
            var assembly = dependencyGraph.Keys.First();

            Console.WriteLine($"Cataloging compatibilities, starting with assembly {assembly.Assembly}");
            CatalogCompatibilities(assembly, dependencyGraph);
        }

        return GetIncompatibleApisUsedInProject();
    }

    private IEnumerable<MethodDefinition> GetIncompatibleApisUsedInProject()
    {
        var projectReferenceAssemblies = GetProjectReferenceAssemblies();
        var methodsCalledInProjectReferenceAssemblies = GetMethodsCalledInProjectReferenceAssemblies(projectReferenceAssemblies);
        var incompatibleMethodsUsedInProject = IncompatibleMethods.Intersect(methodsCalledInProjectReferenceAssemblies);

        return incompatibleMethodsUsedInProject;
    }

    /// <summary>
    /// Returns assemblies that were project references in source code, i.e. the assemblies for projects
    /// that were part of the original solution, excluding SDK and Nuget package assemblies.
    /// </summary>
    /// <returns>Assemblies from project references</returns>
    private ISet<ModuleDefinition> GetProjectReferenceAssemblies()
    {
        // Search the directory of the entry-point dll to find all *.deps.json files.
        // (there is 1 deps.json file per project reference from the original solution)
        var sourceAssemblyDir = Path.GetDirectoryName(_assemblyFile) ?? string.Empty;
        var depsJsonFiles = Directory.EnumerateFiles(sourceAssemblyDir, "*.deps.json", SearchOption.AllDirectories)
            .Select(Path.GetFileName);
        var projectReferenceAssemblyNames = depsJsonFiles
            .Select(depsFile => depsFile.Replace("deps.json", "dll"))
            .ToHashSet();

        var allAssemblies = AssemblyCompatibilities.Keys;
        var projectReferenceAssemblies = allAssemblies
            .Where(assembly => projectReferenceAssemblyNames.Contains(Path.GetFileName(assembly.FileName)))
            .ToHashSet();

        return projectReferenceAssemblies;
    }

    private ISet<MethodDefinition> GetMethodsCalledInProjectReferenceAssemblies(ISet<ModuleDefinition> projectReferenceAssemblies)
    {
        var methodsDeclared = projectReferenceAssemblies
            .SelectMany(assembly => assembly.Types)
            .SelectMany(type => type.Methods)
            .ToHashSet();
        var methodsCalled = methodsDeclared
            .SelectMany(GetInnerMethods)
            .ToHashSet();

        return methodsDeclared.Union(methodsCalled).ToHashSet();
    }

    /// <summary>
    /// Builds an incomplete dependency graph from an assembly reference. All references are included,
    /// but each assembly will only be referenced once. This modification is to optimize for memory usage.
    /// </summary>
    /// <param name="assembly">Assembly to build the graph from</param>
    /// <param name="dependencyGraph">Maps each assembly to a collection of assemblies they reference</param>
    /// <param name="visitedAssemblies">Set of all assemblies in the dependency graph</param>
    /// <returns>The dependency graph</returns>
    public Dictionary<ModuleDefinition, IEnumerable<ModuleDefinition>> BuildDependencyGraph(
        ModuleDefinition assembly,
        Dictionary<ModuleDefinition, IEnumerable<ModuleDefinition>>? dependencyGraph = null,
        HashSet<ModuleDefinition>? visitedAssemblies = null)
    {
        Console.WriteLine($"Tracking dependencies for {assembly.Assembly}");
        dependencyGraph ??= new Dictionary<ModuleDefinition, IEnumerable<ModuleDefinition>>();
        visitedAssemblies ??= new HashSet<ModuleDefinition>();

        var referencedAssemblies = assembly.GetReferencedAssemblies(_assemblyResolver);
        referencedAssemblies.ExceptWith(visitedAssemblies);
        dependencyGraph.TryAdd(assembly, referencedAssemblies);
        visitedAssemblies.UnionWith(referencedAssemblies);
        Console.WriteLine($"Found {referencedAssemblies.Count} untracked child assemblies");

        foreach (var referencedAssembly in referencedAssemblies)
        {
            dependencyGraph = BuildDependencyGraph(referencedAssembly, dependencyGraph, visitedAssemblies);
        }

        return dependencyGraph;
    }

    /// <summary>
    /// Catalogs the compatibilities of all assemblies in the dependency graph, depth-first
    /// </summary>
    /// <param name="assembly">Assembly to find compatibility for</param>
    /// <param name="dependencyGraph">Dependency graph of compatibilities</param>
    /// <exception cref="Exception">Thrown when assembly is not in the dependency graph</exception>
    public void CatalogCompatibilities(ModuleDefinition assembly,
        Dictionary<ModuleDefinition, IEnumerable<ModuleDefinition>> dependencyGraph)
    {
        if (!dependencyGraph.TryGetValue(assembly, out var assemblyReferences))
        {
            throw new Exception($"Assembly {assembly.Assembly} was not found in dependency graph.");
        }

        foreach (var assemblyReference in assemblyReferences)
        {
            CatalogCompatibilities(assemblyReference, dependencyGraph);
        }

        CatalogAssemblyCompatibility(assembly);
    }

    private void CatalogAssemblyCompatibility(ModuleDefinition assembly)
    {
        Console.WriteLine($"Cataloging compatibilities for assembly {assembly.Assembly}");
        var assemblyCompatibility = GetAssemblyCompatibility(assembly);

        var publicClasses = assembly.Types.Where(t => t.IsPublic);

        var publicMethods = publicClasses
            .SelectMany(t => t.Methods)
            .Where(m => m.IsPublic);

        Console.WriteLine($"{publicClasses.Count()} public classes found");
        Console.WriteLine($"{publicMethods.Count()} public methods found");
        CatalogMethodCompatibilities(publicMethods);
    }

    private MetadataModels.AssemblyCompatibility GetAssemblyCompatibility(ModuleDefinition assembly)
    {
        if (AssemblyCompatibilities.TryGetValue(assembly, out var cachedAssemblyCompatibility))
        {
            return cachedAssemblyCompatibility;
        }

        var isNetCoreCompatible = assembly.IsNetCoreCompatible();
        var isLinuxCompatible = assembly.IsLinuxCompatible(isNetCoreCompatible);
        var assemblyCompatibility = new MetadataModels.AssemblyCompatibility
        {
            IsNetCoreCompatible = isNetCoreCompatible,
            IsLinuxCompatible = isLinuxCompatible,
            IsWindowsCompatibleOnly = !isLinuxCompatible
        };

        AssemblyCompatibilities.TryAdd(assembly, assemblyCompatibility);
        return assemblyCompatibility;
    }

    private void CatalogMethodCompatibilities(IEnumerable<MethodDefinition> methods)
    {
        Console.WriteLine($"Cataloging compatibility for {methods.Count()} methods");
        methods.ToList().ForEach(method =>
        {
            var callStack = new HashSet<MethodDefinition>();
            var methodCompatibility = CatalogMethodCompatibility(method, callStack);
            MethodCompatibilities.TryAdd(method, methodCompatibility);
        });
    }

    private MethodCompatibility CatalogMethodCompatibility(
        MethodDefinition method,
        HashSet<MethodDefinition> callStack)
    {
        // Use cached compatibility if it exists
        if (MethodCompatibilities.TryGetValue(method, out var cachedMethodCompatibility))
        {
            return cachedMethodCompatibility;
        }

        // Use assembly compatibility if possible
        var sourceAssembly = method.Module;
        if (AssemblyCompatibilities.TryGetValue(sourceAssembly, out var cachedAssemblyCompatibility))
        {
            if (cachedAssemblyCompatibility.IsNetCoreCompatible is false or null
                || cachedAssemblyCompatibility.IsLinuxCompatible is false or null)
            {
                var methodCompatibilityFromAssembly = new MethodCompatibility(cachedAssemblyCompatibility);
                MethodCompatibilities.TryAdd(method, methodCompatibilityFromAssembly);

                return methodCompatibilityFromAssembly;
            }
        }

        // Check attributes on method for compatibility
        var isLinuxCompatible = method.IsLinuxCompatible();
        var methodCompatibility = new MethodCompatibility
        {
            IsLinuxCompatible = isLinuxCompatible,
            IsNetCoreCompatible = true,
            IsWindowsCompatibleOnly = !isLinuxCompatible
        };

        /*
         So far, method compatibility has only been determined by the attributes on the method declaration. 
         If method is from a Microsoft SDK dll, this should be sufficient as Microsoft's labels on method
         declarations serve as "ground truth" for platform-dependent attribute labeling.
        */
        if (IsMethodFromMicrosoft(method))
        {
            MethodCompatibilities.TryAdd(method, methodCompatibility);
            return methodCompatibility;
        }

        /*
         If the method is not from a Microsoft SDK or dll, we must ensure that all methods called
         within the method are linux-compatible in order for it to be linux-compatible itself. This
         is a recursive procedure.
         */
        var innerMethods = GetInnerMethods(method);
        var innerMethodsToProcess = innerMethods.Except(callStack).ToHashSet();
        var innerMethodCompatibilities = innerMethodsToProcess.Select(innerMethod =>
        {
            callStack.Add(innerMethod);
            var innerMethodCompatibility = CatalogMethodCompatibility(innerMethod, callStack);
            callStack.Remove(innerMethod);

            MethodCompatibilities.TryAdd(innerMethod, innerMethodCompatibility);
            return innerMethodCompatibility;
        });

        // Use the method compatibilities from the inner methods to determine this method's compatibility
        if (innerMethodCompatibilities.Any())
        {
            isLinuxCompatible = innerMethodCompatibilities.All(c => c.IsLinuxCompatible == true);
            methodCompatibility.IsLinuxCompatible = isLinuxCompatible;
            methodCompatibility.IsWindowsCompatibleOnly = !isLinuxCompatible;
        }
        
        MethodCompatibilities.TryAdd(method, methodCompatibility);
        return methodCompatibility;
    }

    private bool IsMethodFromMicrosoft(MethodDefinition method)
    {
        var sourceDllName = Path.GetFileName(method.Module.FileName);
        return _assembliesFromMicrosoft.Contains(sourceDllName);
    }

    private ISet<MethodDefinition> GetInnerMethods(MethodDefinition method)
    {
        if (method.Body?.Instructions == null)
        {
            return new HashSet<MethodDefinition>();
        }

        return method.Body.Instructions
            .Where(i => i.OpCode.Code is Code.Call or Code.Callvirt)
            .Select(i => i.Operand as MethodReference)
            .Where(m => m != null)
            .Select(m => m.Resolve())
            .Where(m => m != null)
            .ToHashSet();
    }

    //public void CatalogAssemblies(IEnumerable<ModuleDefinition> moduleDefinitions)
    //{
    //    Console.WriteLine($"Cataloging {moduleDefinitions.Count()} assemblies");
    //    moduleDefinitions.ToList().ForEach(CatalogAssembly);
    //}

    //public void CatalogAssembly(string assemblyFile)
    //{
    //    try
    //    {
    //        var moduleDefinition = ModuleDefinition.ReadModule(assemblyFile, _readerParameters);
    //        CatalogAssembly(moduleDefinition);
    //    }
    //    catch (Exception e)
    //    {
    //        Console.WriteLine(e);
    //    }
    //}

    //public void CatalogAssemblies(IEnumerable<string> assemblyFiles)
    //{
    //    Console.WriteLine($"Cataloging {assemblyFiles.Count()} assemblies");
    //    assemblyFiles.ToList().ForEach(assemblyFile =>
    //    {
    //        try
    //        {
    //            var moduleDefinition = ModuleDefinition.ReadModule(assemblyFile, _readerParameters);

    //            CatalogAssembly(moduleDefinition);
    //        }
    //        catch (Exception e)
    //        {
    //            Console.WriteLine(e);
    //        }
    //    });
    //}

    //public void CatalogAssembly(ModuleDefinition? moduleDefinition)
    //{
    //    if (moduleDefinition == null)
    //    {
    //        return;
    //    }

    //    var dependencyGraph = BuildDependencyGraph(moduleDefinition);

    //    Catalog.AddAssembly(moduleDefinition);

    //    var types = moduleDefinition.Types.Where(t => t.IsPublic);
    //    Catalog.AddClasses(types);

    //    var methods = types
    //        .SelectMany(t => t.Methods)
    //        .Where(m => m.IsPublic);
    //    Catalog.AddMethods(methods);

    //    CatalogMethodCompatibilities(methods);

    //    foreach (var method in methods)
    //    {
    //        // CatalogMethodsDepthFirst
    //        if (!method.HasBody)
    //        {
    //            continue;
    //        }
    //        var instructions = method.Body.Instructions;
    //        var cSharpMethods = ILMethodHelper.ConvertToCSharpMethod(method);


    //    }

    //    var assemblyReferences = moduleDefinition.AssemblyReferences ?? new Collection<AssemblyNameReference>();
    //    var resolvedModuleDefinitions = assemblyReferences.SelectMany(assemblyReference =>
    //    {
    //        try
    //        {
    //            return _assemblyResolver.Resolve(assemblyReference).Modules;
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine(ex);
    //            return new Collection<ModuleDefinition>();
    //        }
    //    });
    //    resolvedModuleDefinitions = resolvedModuleDefinitions
    //        .Where(r => !Catalog.NugetAssemblies.ContainsKey(r.FileName))
    //        .Where(r => !Catalog.SdkAssemblies.ContainsKey(r.FileName));
    //    CatalogAssemblies(resolvedModuleDefinitions);
    //}

    //private void CatalogClasses(ModuleDefinition moduleDefinition, Collection<TypeDefinition> types)
    //{
    //    foreach (var type in types)
    //    {
    //        if (Catalog.IsAlreadyProcessed(type))
    //        {
    //            continue;
    //        }
    //        Catalog.AddClass(moduleDefinition, type);

    //        var methods = type.Methods ?? new Collection<MethodDefinition>();
    //        foreach (var method in methods)
    //        {
    //            if (Catalog.IsAlreadyProcessed(method))
    //            {
    //                continue;
    //            }
    //            CatalogMethod(moduleDefinition, type, method);
    //        }
    //    }
    //}

    //public void CatalogMethod(ModuleDefinition moduleDefinition, TypeDefinition type, MethodDefinition method)
    //{
    //    Catalog.AddMethod(moduleDefinition, type, method);

    //    var ilInstructions = method.Body.Instructions;
    //    // convert ilInstructions to methodDefinition
    //    // Catalog the method instruction
    //    // Determine compatibility

    //}

    //public bool IsUnsupportedApi(string methodSignature)
    //{
    //    var signature = NormalizeMethodSignature(methodSignature);

    //    return _publicUnsupportedMethods.Contains(signature);
    //}

    //public bool IsApiDefined(string methodSignature)
    //{
    //    var signature = NormalizeMethodSignature(methodSignature);

    //    return _methodsIL2CSharpMap.ContainsValue(signature);
    //}

    //private string NormalizeMethodSignature(string methodSignature)
    //{
    //    var signature = methodSignature;

    //    /* Handle Nullable parameters containing '?' */
    //    if (signature.Contains(Reference))
    //    {
    //        signature = signature.Replace(Reference, "");
    //    }

    //    if (methodSignature.Contains(ILTypeMapper.SystemDecimal))
    //    {
    //        signature = signature.Replace(ILTypeMapper.SystemDecimal,
    //            ILTypeMapper.SystemDecimalShort);
    //    }

    //    return signature;
    //}

    //public void ComputeUnsupportedApis()
    //{
    //    foreach (TypeDefinition klass in _moduleDefinition.Types)
    //    {
    //        try
    //        {
    //            TraverseAllClasses(klass, new HashSet<string>());
    //        }
    //        catch (AssemblyResolutionException e)
    //        {
    //            // This exception will occur when a referenced API belongs to a dependency that is not packaged with the nuget package
    //            // (in Visual Studio, these dependencies will be automatically resolved but there is no known way of detecting these
    //            // dependencies using the Nuget.Client package).
    //            // This is quite common so logging the message is commented out to prevent excess log growth. Logging can be re-enabled
    //            // for debugging purposes.
    //            //Console.WriteLine($"Failed to resolve dependency used by class {klass}. Dependency may have been excluded from the package: {e.Message}");
    //        }
    //        catch (Exception e)
    //        {
    //            Console.WriteLine(e);
    //            throw;
    //        }
    //    }

    //    if (_isDebug)
    //    {
    //        Console.WriteLine("IL to CSharp methods mapping ");
    //        Console.WriteLine("--------------------------------------------------------------");

    //        foreach (var kv in _methodsIL2CSharpMap)
    //        {
    //            Console.WriteLine(kv.Key + " --> " + kv.Value);
    //        }

    //        Console.WriteLine("publicUnsupportedMethods : " + _unsupportedMethodsCount);
    //        Console.WriteLine(string.Join("\n", _publicUnsupportedMethods));
    //    }
    //}

    //private void TraverseAllClasses(TypeDefinition klass, HashSet<string> visited)
    //{
    //    /* Some methods are missing if we block this */
    //    if (!klass.IsPublic)
    //    {
    //        return;
    //    }

    //    if (visited.Contains(klass.FullName))
    //    {
    //        return;
    //    }

    //    AddConstuctor(klass);
    //    foreach (var method in klass.Methods)
    //    {
    //        TraverseAllMethods(klass, method);
    //    }

    //    //if (!klass.HasNestedTypes|| !klass.IsNested) return;

    //    foreach (var nested in klass.NestedTypes)
    //    {
    //        visited.Add(klass.FullName);
    //        TraverseAllClasses(nested, visited);
    //        visited.Remove(klass.FullName);
    //    }
    //}

    //private void AddConstuctor(TypeDefinition klass)
    //{
    //    string cname = ILMethodHelper.ConvertToCSharpConstructor(klass);
    //    if (!string.IsNullOrEmpty(cname))
    //        _methodsIL2CSharpMap[cname] = cname;
    //}

    //private void TraverseAllMethods(TypeDefinition klass, MethodDefinition method)
    //{
    //    /*if (!method.IsPublic)
    //        return;
    //    */
    //    var info = ScanPlatformNotSupported(method, new HashSet<string>(), 0);

    //    var cSharpMethods = ILMethodHelper.ConvertToCSharpMethod(method);

    //    if (_methodsIL2CSharpMap.ContainsKey(method.FullName))
    //    {
    //        _methodsIL2CSharpMap[_methodsIL2CSharpMap[method.FullName]] = cSharpMethods[0];
    //    }
    //    else
    //    {
    //        _methodsIL2CSharpMap[method.FullName] = cSharpMethods[0];
    //    }

    //    if (cSharpMethods.Count > 1)
    //    {
    //        // Sometimes we need to map two different methods: 1) Without translation 2) With translation
    //        // For example: Class.Method.get_Info() and Class.Method.Info.get
    //        _methodsIL2CSharpMap[cSharpMethods[0]] = cSharpMethods[1];
    //    }

    //    if (info.Level != -1)
    //    {
    //        _unsupportedMethodsCount++;

    //        _publicUnsupportedMethods.AddRange(cSharpMethods);
    //    }
    //}

    ///* Some part of the code was taken  from: https://github.com/dotnet/platform-compat/tree/master/src/ex-scan
    //   and modified to use  Mono.Cecil library.
    // */
    //private ExceptionInfo ScanPlatformNotSupported(MethodDefinition method, HashSet<string> visited, int nestingLevel = 0)
    //{

    //    const int maxNestingLevel = 3;

    //    if (method == null || method.IsAbstract || HasIntrinsicAttribute(method))
    //    {
    //        return ExceptionInfo.DoesNotThrow;
    //    }
    //    if (visited.Contains(method.FullName)) return ExceptionInfo.DoesNotThrow;
    //    if (ApiExcludesLinux(method))
    //    {
    //        return ExceptionInfo.ThrowsAt(nestingLevel, method.FullName);
    //    }

    //    foreach (var op in GetOperationsPreceedingThrow(method))
    //    {
    //        // throw new PlatformNotSupportedExeption(...)
    //        if (op.OpCode.Code == Code.Newobj &&
    //            op.Operand is MethodReference m &&
    //            IsPlatformNotSupported(op.Operand.ToString()))
    //        {
    //            return ExceptionInfo.ThrowsAt(nestingLevel, method.FullName);
    //        }

    //        // throw SomeFactoryForPlatformNotSupportedExeption(...);
    //        if (op.Operand is MethodReference r &&
    //            IsFactoryForPlatformNotSupported(r))
    //        {
    //            return ExceptionInfo.ThrowsAt(nestingLevel, method.FullName);
    //        }
    //    }

    //    var result = ExceptionInfo.DoesNotThrow;

    //    if (nestingLevel < maxNestingLevel)
    //    {
    //        foreach (var calledMethod in GetCalls(method.Resolve()))
    //        {
    //            bool val = ResolveMethod(calledMethod);
    //            if (!val) continue;
    //            var nextMethod = calledMethod.Resolve();
    //            visited.Add(nextMethod.FullName);
    //            var nestedResult = ScanPlatformNotSupported(nextMethod, visited, nestingLevel + 1);
    //            visited.Remove(nextMethod.FullName);
    //            result = result.Combine(nestedResult);
    //        }
    //    }

    //    return result;
    //}

    //private bool ResolveMethod(MethodReference calledMethod)
    //{
    //    try
    //    {
    //        var resolveTask = Task.Run(() => calledMethod.Resolve());
    //        if (resolveTask.Wait(TimeSpan.FromSeconds(2)))
    //            return true;
    //        else
    //        {
    //            return false;
    //        }
    //    }
    //    catch (AssemblyResolutionException e)
    //    {
    //        // This exception will occur when a referenced API belongs to a dependency that is not packaged with the nuget package
    //        // (in Visual Studio, these dependencies will be automatically resolved but there is no known way of detecting these
    //        // dependencies using the Nuget.Client package).
    //        // This is quite common so logging the message is commented out to prevent excess log growth. Logging can be re-enabled
    //        // for debugging purposes.
    //        //Console.WriteLine($"Failed to resolve dependency used by method {calledMethod}. Dependency may have been excluded from the package: {e.Message}");
    //    }
    //    catch (Exception e)
    //    {
    //        Console.WriteLine(calledMethod.FullName);
    //        Console.WriteLine(e);
    //    }

    //    return false;
    //}

    //private bool HasIntrinsicAttribute(MethodDefinition method)
    //{
    //    var name = method.FullName;

    //    return (method.HasCustomAttributes
    //            && method.CustomAttributes.Any(attrb =>
    //                attrb != null
    //                && "System.Runtime.CompilerServices.IntrinsicAttribute".Equals(attrb.AttributeType.FullName)))
    //           || name.StartsWith("System.Runtime.Intrinsics")
    //           || name.StartsWith("System.ByReference");
    //}

    //private static bool ApiExcludesLinux(MethodDefinition method)
    //{
    //    if (!method.HasCustomAttributes)
    //    {
    //        return false;
    //    }

    //    var attrs = method.CustomAttributes;
    //    return attrs.Any(a =>
    //    {
    //        // If there is a platform inclusion list and linux is not included, return true
    //        if (a.AttributeType.Name == "SupportedOSPlatformAttribute"
    //            && a.ConstructorArguments.All(arg => arg.Value?.ToString() != "linux"))
    //        {
    //            return true;
    //        }

    //        // If there is a platform exclusion list and linux is included, return true
    //        if (a.AttributeType.Name == "UnsupportedOSPlatformAttribute"
    //            && a.ConstructorArguments.Any(arg => arg.Value?.ToString() == "linux"))
    //        {
    //            return true;
    //        }

    //        // By default, return false
    //        return false;
    //    });
    //}

    //private IEnumerable<Instruction> GetOperationsPreceedingThrow(MethodDefinition method)
    //{
    //    Instruction previous = null;

    //    if (method.Body != null &&
    //        method.Body.Instructions != null)
    //    {

    //        foreach (var op in method.Body.Instructions)
    //        {
    //            if (op.OpCode.Code == Code.Nop)
    //                continue;

    //            if (op.OpCode.Code == Code.Throw && previous != null)
    //                yield return previous;

    //            previous = op;
    //        }
    //    }
    //}

    //private IEnumerable<MethodReference> GetCalls(MethodDefinition method)
    //{
    //    if (method.Body == null ||
    //        method.Body.Instructions == null)
    //    {
    //        return Enumerable.Empty<MethodReference>();
    //    }

    //    return method.Body.Instructions.Where(o => o.OpCode.Code == Code.Call ||
    //                                             o.OpCode.Code == Code.Callvirt)
    //                                 .Select(o => o.Operand as MethodReference)
    //                                 .Where(m => m != null);
    //}

    //private bool IsPlatformNotSupported(string method)
    //{
    //    return UNSUPPORTED_EXCEPTIONS.Contains(method);
    //}

    //private bool IsFactoryForPlatformNotSupported(MethodReference reference)
    //{
    //    var methodInfo = reference.Resolve();

    //    if (methodInfo == null || methodInfo.IsAbstract)
    //        return false;

    //    //MethodReference constructorReference = null;
    //    Instruction constructorReference = null;

    //    foreach (var op in methodInfo.Body.Instructions)
    //    {
    //        switch (op.OpCode.Code)
    //        {
    //            case Code.Newobj:
    //                constructorReference = op;
    //                break;
    //            case Code.Ret:
    //                if (constructorReference != null &&
    //                    IsPlatformNotSupported(constructorReference.Operand.ToString()))
    //                    return true;
    //                break;
    //        }
    //    }

    //    return false;
    //}
}