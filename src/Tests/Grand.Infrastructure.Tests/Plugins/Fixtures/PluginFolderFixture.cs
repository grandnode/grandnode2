using Grand.Infrastructure.Plugins;
using System.Runtime.Loader;

namespace Grand.Infrastructure.Tests.Plugins.Fixtures;

/// <summary>
///     A temporary Plugins folder and a collectible load context for plugin loading tests. Disposing
///     unloads the context and removes the folder.
/// </summary>
public sealed class PluginFolderFixture : IDisposable
{
    public static readonly string CoreAssemblyPath = typeof(PluginInfoAttribute).Assembly.Location;
    private static readonly HashSet<string> Host = new(StringComparer.OrdinalIgnoreCase) { "Grand.Infrastructure" };

    private readonly string _root;

    public PluginFolderFixture()
    {
        _root = Path.Combine(Path.GetTempPath(), "grand-plugin-tests", Guid.NewGuid().ToString("N"));
        PluginsFolder = Path.Combine(_root, "Plugins");
        Directory.CreateDirectory(PluginsFolder);
        Context = new AssemblyLoadContext("plugin-tests-" + Guid.NewGuid().ToString("N"), true);
    }

    public string PluginsFolder { get; }

    public AssemblyLoadContext Context { get; }

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
        return PluginLoader.Load(Candidate(folder), installed, Context, x => x);
    }

    public System.Reflection.Assembly Loaded(string assemblyName) =>
        Context.Assemblies.Single(x => x.GetName().Name == assemblyName);

    public void Dispose()
    {
        Context.Unload();
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
