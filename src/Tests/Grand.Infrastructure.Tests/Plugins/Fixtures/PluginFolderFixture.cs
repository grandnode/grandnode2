using Grand.Infrastructure.Plugins;
using System.Runtime.Loader;

namespace Grand.Infrastructure.Tests.Plugins.Fixtures;

/// <summary>
///     A temporary Plugins folder for plugin loading tests, whose plugins load into collectible contexts of
///     their own. Disposing unloads every context and removes the folder.
/// </summary>
public sealed class PluginFolderFixture : IDisposable
{
    public static readonly string CoreAssemblyPath = typeof(PluginInfoAttribute).Assembly.Location;
    private static readonly HashSet<string> Host = new(StringComparer.OrdinalIgnoreCase) { "Grand.Infrastructure" };

    private readonly string _root;
    private readonly List<AssemblyLoadContext> _contexts = [];

    public PluginFolderFixture()
    {
        _root = Path.Combine(Path.GetTempPath(), "grand-plugin-tests", Guid.NewGuid().ToString("N"));
        PluginsFolder = Path.Combine(_root, "Plugins");
        Directory.CreateDirectory(PluginsFolder);
    }

    public string PluginsFolder { get; }

    /// <summary>
    ///     The contexts created so far, one per plugin loaded
    /// </summary>
    public IReadOnlyList<AssemblyLoadContext> Contexts
    {
        get
        {
            lock (_contexts)
            {
                return _contexts.ToList();
            }
        }
    }

    /// <summary>
    ///     A plugin's own context, as production creates it, but collectible
    /// </summary>
    public AssemblyLoadContext CreateContext(string name, string mainAssemblyPath, ISet<string> hostAssemblyNames)
    {
        var context = new IsolatedLoadContext("plugin-tests-" + name, mainAssemblyPath, hostAssemblyNames, true);
        lock (_contexts)
        {
            _contexts.Add(context);
        }

        return context;
    }

    public string Folder(string name) => Path.Combine(PluginsFolder, name);

    /// <summary>
    ///     Compiles a plugin assembly carrying <see cref="PluginInfoAttribute" /> with <paramref name="source" /> beside it.
    /// </summary>
    public string AddPlugin(string folder, string assemblyName, string systemName, string source = "",
        params string[] references)
    {
        return TestPluginBuilder.Compile(Folder(folder), assemblyName, $$"""
            using Grand.Infrastructure.Plugins;
            [assembly: PluginInfo(SystemName = "{{systemName}}")]
            {{source}}
            """, null, [CoreAssemblyPath, .. references]);
    }

    /// <summary>
    ///     Compiles an assembly outside the Plugins folder, to compile a plugin against without shipping it.
    /// </summary>
    public string CompileElsewhere(string assemblyName, string source)
    {
        return TestPluginBuilder.Compile(Path.Combine(_root, "elsewhere"), assemblyName, source);
    }

    public PluginScanResult Scan() => PluginFolderScanner.Scan(new DirectoryInfo(PluginsFolder), null, Host);

    public PluginCandidate Candidate(string folder) => Scan().Plugins.Single(x => x.Folder.Name == folder);

    public PluginInfo Load(string folder, bool installed)
    {
        var hostAssemblyNames = PluginManager.HostAssemblyNames();
        return PluginLoader.Load(Candidate(folder), installed,
            main => CreateContext(folder, main, hostAssemblyNames), x => x.Folder);
    }

    /// <summary>
    ///     The one assembly of that name loaded by any of the plugins
    /// </summary>
    public System.Reflection.Assembly Loaded(string assemblyName) =>
        Contexts.SelectMany(x => x.Assemblies).Single(x => x.GetName().Name == assemblyName);

    /// <summary>
    ///     The assembly of that name in the plugin's own context
    /// </summary>
    public static System.Reflection.Assembly Loaded(PluginInfo plugin, string assemblyName) =>
        AssemblyLoadContext.GetLoadContext(plugin.ReferencedAssembly)!.Assemblies
            .Single(x => x.GetName().Name == assemblyName);

    public void Dispose()
    {
        foreach (var context in Contexts)
            context.Unload();
        for (var i = 0; i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        try
        {
            Directory.Delete(_root, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            //a file the context still maps is released once the context is collected; the folder is in temp
        }
    }
}
