using System.Reflection;
using System.Runtime.Loader;

namespace Grand.Infrastructure.Plugins;

/// <summary>
///     The load context of one extension (a plugin or a module). It resolves the extension's own dependencies
///     from its deps.json, or from its folder, and takes every assembly the application provides from the
///     default context, so the extension shares the host's types while its private libraries, in whatever
///     version it ships them, stay its own.
/// </summary>
/// <remarks>Not collectible: an extension stays loaded until the application stops.</remarks>
public sealed class IsolatedLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _folder;
    private readonly ISet<string> _hostAssemblyNames;

    /// <param name="name">The context's name, for diagnostics</param>
    /// <param name="mainAssemblyPath">The extension's main assembly; its deps.json, if any, sits beside it</param>
    /// <param name="hostAssemblyNames">Simple names of the assemblies the application provides</param>
    /// <exception cref="InvalidOperationException">The extension's deps.json cannot be resolved</exception>
    public IsolatedLoadContext(string name, string mainAssemblyPath, ISet<string> hostAssemblyNames)
        : this(name, mainAssemblyPath, hostAssemblyNames, false)
    {
    }

    /// <param name="isCollectible">Tests only: lets a test unload what it loaded</param>
    internal IsolatedLoadContext(string name, string mainAssemblyPath, ISet<string> hostAssemblyNames,
        bool isCollectible) : base(name, isCollectible)
    {
        ArgumentException.ThrowIfNullOrEmpty(mainAssemblyPath);
        ArgumentNullException.ThrowIfNull(hostAssemblyNames);

        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
        _folder = Path.GetDirectoryName(Path.GetFullPath(mainAssemblyPath));
        _hostAssemblyNames = hostAssemblyNames;
    }

    /// <summary>
    ///     Null hands the name to the default context: an assembly the application provides, or one it has
    ///     already loaded, is never loaded a second time.
    /// </summary>
    protected override Assembly Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        if (string.IsNullOrEmpty(name) || _hostAssemblyNames.Contains(name) || IsLoadedInDefault(name))
            return null;

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path != null)
            return LoadFromAssemblyPath(path);

        //a dll that sits in the folder without being listed in deps.json (or there is no deps.json)
        var local = Path.Combine(_folder, name + ".dll");
        return File.Exists(local) ? LoadFromAssemblyPath(local) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }

    private static bool IsLoadedInDefault(string name)
    {
        return Default.Assemblies.Any(x => string.Equals(x.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
