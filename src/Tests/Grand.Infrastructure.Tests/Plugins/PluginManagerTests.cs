using Grand.Domain;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Tests.Plugins.Fixtures;
using Grand.SharedKernel;
using LiteDB;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections;
using System.Reflection;
using System.Runtime.Loader;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
[DoNotParallelize]
public class PluginManagerTests
{
    private static readonly string DomainAssemblyPath = typeof(BaseEntity).Assembly.Location;

    private PluginFolderFixture _fixture;
    private IEnumerable<PluginInfo> _referencedPlugins;
    private PluginLoadSnapshot _loadSnapshot;

    [TestInitialize]
    public void Init()
    {
        _fixture = new PluginFolderFixture();
        _referencedPlugins = PluginManager.ReferencedPlugins;
        _loadSnapshot = PluginManager.LoadSnapshot;
    }

    [TestCleanup]
    public void Cleanup()
    {
        PluginManager.ReferencedPlugins = _referencedPlugins;
        PluginManager.LoadSnapshot = _loadSnapshot;
        _fixture.Dispose();
    }

    [TestMethod]
    public void Load_UninstalledPlugin_IsListedButNotLoaded()
    {
        var main = _fixture.AddPlugin("Uninstalled", "Manager.Uninstalled", "Manager.Uninstalled",
            "public class UninstalledPlugin : BasePlugin { }");
        TestPluginBuilder.Compile(_fixture.Folder("Uninstalled"), "Manager.Uninstalled.Core", "public class Core { }");

        Load();

        var plugin = Plugin("Manager.Uninstalled");
        Assert.IsFalse(plugin.Installed);
        Assert.IsNull(plugin.LoadError);
        Assert.IsNull(plugin.ReferencedAssembly);
        Assert.IsNull(plugin.PluginType);
        //the list and the logo come from the scan
        Assert.AreEqual(main, plugin.OriginalAssemblyFile.FullName);
        Assert.Contains("Manager.Uninstalled.Core", plugin.AssemblyNames);
        Assert.IsFalse(IsLoaded("Manager.Uninstalled"));
        Assert.IsFalse(IsLoaded("Manager.Uninstalled.Core"));
    }

    [TestMethod]
    public void Load_InstalledPlugin_IsLoaded()
    {
        _fixture.AddPlugin("Installed", "Manager.Installed", "Manager.Installed",
            "public class InstalledPlugin : BasePlugin { }");

        Load("Manager.Installed");

        var plugin = Plugin("Manager.Installed");
        Assert.IsTrue(plugin.IsActive);
        Assert.AreEqual("InstalledPlugin", plugin.PluginType.Name);
        Assert.AreSame(_fixture.Loaded("Manager.Installed"), plugin.ReferencedAssembly);
    }

    [TestMethod]
    public void Load_IncompatiblePlugin_IsListedButNotLoaded()
    {
        TestPluginBuilder.Compile(_fixture.Folder("Old"), "Manager.Old", """
            using Grand.Infrastructure.Plugins;
            [assembly: PluginInfo(SystemName = "Manager.Old", SupportedVersion = "1.0")]
            public class OldPlugin : BasePlugin { }
            """, null, PluginFolderFixture.CoreAssemblyPath);

        Load("Manager.Old");

        var plugin = Plugin("Manager.Old");
        Assert.IsFalse(plugin.Installed);
        Assert.IsNull(plugin.ReferencedAssembly);
        Assert.IsNull(plugin.PluginType);
        Assert.IsNotNull(plugin.OriginalAssemblyFile);
        //nor on demand: a plugin built for another version is never installed
        using var provider = new ServiceCollection().BuildServiceProvider();
        Assert.ThrowsExactly<GrandException>(() => plugin.Instance<IPlugin>(provider));
        Assert.IsFalse(IsLoaded("Manager.Old"));
    }

    [TestMethod]
    public void Instance_UninstalledPlugin_LoadsOnDemandAndActivates()
    {
        _fixture.AddPlugin("OnDemand", "Manager.OnDemand", "Manager.OnDemand", """
            public class OnDemandPlugin(System.IServiceProvider services) : BasePlugin
            {
                public System.IServiceProvider Services { get; } = services;
            }
            """);
        Load();
        var plugin = Plugin("Manager.OnDemand");

        //an uninstalled plugin is not registered in DI
        using var provider = new ServiceCollection().BuildServiceProvider();
        var instance = plugin.Instance<IPlugin>(provider);

        Assert.AreEqual("OnDemandPlugin", instance.GetType().Name);
        Assert.AreSame(plugin, instance.PluginInfo);
        Assert.IsNull(plugin.LoadError);
        Assert.AreSame(_fixture.Loaded("Manager.OnDemand"), plugin.ReferencedAssembly);
        Assert.AreSame(instance.GetType(), plugin.PluginType);
        //installed only after the restart: until then it contributes no types to any scan
        Assert.IsFalse(plugin.Installed);
        Assert.IsFalse(PluginExtensions.OnlyInstalledPlugins(plugin.ReferencedAssembly));
    }

    [TestMethod]
    public void Instance_ConcurrentCalls_LoadThePluginOnce()
    {
        _fixture.AddPlugin("Concurrent", "Manager.Concurrent", "Manager.Concurrent",
            "public class ConcurrentPlugin : BasePlugin { }");
        Load();
        var plugin = Plugin("Manager.Concurrent");
        using var provider = new ServiceCollection().BuildServiceProvider();

        var types = new Type[8];
        Parallel.For(0, types.Length, i => types[i] = plugin.Instance<IPlugin>(provider).GetType());

        Assert.IsNull(plugin.LoadError);
        Assert.AreEqual(1, types.Distinct().Count());
        Assert.HasCount(1, _fixture.Contexts);
        Assert.HasCount(1, _fixture.Contexts.Single().Assemblies.Where(x => x.GetName().Name == "Manager.Concurrent"));
    }

    [TestMethod]
    public void Instance_UninstalledPluginThatCannotLoad_KeepsLoadErrorAndThrows()
    {
        var missing = _fixture.CompileElsewhere("Manager.MissingDep", "namespace Missing { public class Base { } }");
        _fixture.AddPlugin("Broken", "Manager.Broken", "Manager.Broken", """
            public class Derived : Missing.Base { }
            public class BrokenPlugin : BasePlugin { }
            """, missing);
        Load();
        var plugin = Plugin("Manager.Broken");
        using var provider = new ServiceCollection().BuildServiceProvider();

        var ex = Assert.ThrowsExactly<GrandException>(() => plugin.Instance<IPlugin>(provider));

        Assert.IsNotNull(plugin.LoadError);
        Assert.Contains("Manager.MissingDep", plugin.LoadError);
        Assert.Contains(plugin.LoadError, ex.Message);
        Assert.IsNull(plugin.PluginType);
        Assert.IsNull(plugin.ReferencedAssembly);
    }

    [TestMethod]
    public void Instance_OnLiteDb_BuildsTheMappersOfThePluginLoadedOnDemand()
    {
        //installing a plugin uses its entities on the running site, among requests reading the same mappers
        _fixture.AddPlugin("Lite", "Manager.Lite", "Manager.Lite", """
            public class LitePluginEntity : Grand.Domain.BaseEntity { }
            public class LitePlugin : BasePlugin { }
            """, DomainAssemblyPath);
        Load(useLiteDb: true);
        var plugin = Plugin("Manager.Lite");
        Assert.DoesNotContain("LitePluginEntity", MappedTypeNames());
        using var provider = new ServiceCollection().BuildServiceProvider();

        plugin.Instance<IPlugin>(provider);

        Assert.Contains("LitePluginEntity", MappedTypeNames());
    }

    [TestMethod]
    public void Load_ShadowCopy_LoadsTheWholeFolderFromItsOwnCopy()
    {
        var main = _fixture.AddPlugin("Shadow", "Manager.Shadow", "Manager.Shadow",
            "public class ShadowPlugin : BasePlugin { }");
        WriteDepsJson(_fixture.Folder("Shadow"), "Manager.Shadow");
        Directory.CreateDirectory(Path.Combine(_fixture.Folder("Shadow"), "runtimes", "any"));
        File.WriteAllText(Path.Combine(_fixture.Folder("Shadow"), "runtimes", "any", "native.txt"), "native");
        //left by an earlier start: a file the plugin no longer ships, another plugin's copy, the old flat layout
        Directory.CreateDirectory(ShadowFolder("Shadow"));
        File.WriteAllText(Path.Combine(ShadowFolder("Shadow"), "Removed.dll"), "old");
        Directory.CreateDirectory(ShadowFolder("Other"));
        File.WriteAllText(Path.Combine(ShadowFolder("Other"), "Other.dll"), "other");
        File.WriteAllText(Path.Combine(_fixture.PluginsFolder, "bin", "Flat.dll"), "flat");

        Load("Manager.Shadow", shadowCopy: true);

        var plugin = Plugin("Manager.Shadow");
        Assert.IsNull(plugin.LoadError);
        Assert.AreEqual(Path.Combine(ShadowFolder("Shadow"), "Manager.Shadow.dll"), plugin.ReferencedAssembly.Location);
        Assert.AreEqual(main, plugin.OriginalAssemblyFile.FullName);
        Assert.IsTrue(File.Exists(Path.Combine(ShadowFolder("Shadow"), "Manager.Shadow.deps.json")));
        Assert.IsTrue(File.Exists(Path.Combine(ShadowFolder("Shadow"), "runtimes", "any", "native.txt")));
        Assert.IsFalse(File.Exists(Path.Combine(ShadowFolder("Shadow"), "Removed.dll")));
        Assert.IsTrue(File.Exists(Path.Combine(ShadowFolder("Other"), "Other.dll")));
        Assert.IsFalse(File.Exists(Path.Combine(_fixture.PluginsFolder, "bin", "Flat.dll")));
        //the plugin's own folder keeps its deps.json
        Assert.IsTrue(File.Exists(Path.Combine(_fixture.Folder("Shadow"), "Manager.Shadow.deps.json")));
    }

    [TestMethod]
    public void Load_ShadowCopy_CopiesOnlyFilesThatChanged()
    {
        _fixture.AddPlugin("Copy", "Manager.Copy", "Manager.Copy", "public class CopyPlugin : BasePlugin { }");
        var unchanged = Path.Combine(_fixture.Folder("Copy"), "unchanged.txt");
        var changed = Path.Combine(_fixture.Folder("Copy"), "changed.txt");
        File.WriteAllText(unchanged, "source");
        File.WriteAllText(changed, "source");
        Load("Manager.Copy", shadowCopy: true);
        //same size and time as its source: taken as up to date, so the marker survives the next start
        var unchangedCopy = Path.Combine(ShadowFolder("Copy"), "unchanged.txt");
        File.WriteAllText(unchangedCopy, "marker");
        File.SetLastWriteTimeUtc(unchangedCopy, File.GetLastWriteTimeUtc(unchanged));
        File.WriteAllText(changed, "a newer, longer source");

        Load("Manager.Copy", shadowCopy: true);

        Assert.AreEqual("marker", File.ReadAllText(unchangedCopy));
        Assert.AreEqual("a newer, longer source", File.ReadAllText(Path.Combine(ShadowFolder("Copy"), "changed.txt")));
        Assert.IsTrue(Plugin("Manager.Copy").IsActive);
    }

    [TestMethod]
    public void Load_ShadowCopyOff_LoadsFromThePluginFolderAndKeepsDepsJson()
    {
        var main = _fixture.AddPlugin("Direct", "Manager.Direct", "Manager.Direct",
            "public class DirectPlugin : BasePlugin { }");
        WriteDepsJson(_fixture.Folder("Direct"), "Manager.Direct");

        Load("Manager.Direct");

        var plugin = Plugin("Manager.Direct");
        Assert.IsTrue(plugin.IsActive);
        Assert.AreEqual(main, plugin.ReferencedAssembly.Location);
        Assert.IsTrue(File.Exists(Path.Combine(_fixture.Folder("Direct"), "Manager.Direct.deps.json")));
        Assert.IsFalse(Directory.Exists(ShadowFolder("Direct")));
    }

    [TestMethod]
    public void Load_LogsToTheLoggingProvidersOfTheServices()
    {
        var logs = new CapturingLoggerProvider();

        Load(logs: logs);

        Assert.IsTrue(logs.Entries.Any(x =>
            x.Category == "PluginManager" && x.Message.Contains("Creating shadow copy folder")));
    }

    private void Load(string installed = "Manager.NotInstalled", bool useLiteDb = false, bool shadowCopy = false,
        CapturingLoggerProvider logs = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> {
                ["Extensions:InstalledPlugins"] = installed,
                ["Extensions:PluginShadowCopy"] = shadowCopy.ToString(),
                ["Database:UseLiteDb"] = useLiteDb.ToString()
            })
            .Build();
        var hostEnvironment = new Mock<IWebHostEnvironment>();
        hostEnvironment.Setup(x => x.ContentRootPath).Returns(Path.GetDirectoryName(_fixture.PluginsFolder)!);
        var mvcCoreBuilder = new ServiceCollection().AddLogging(x =>
        {
            if (logs != null)
                x.AddProvider(logs);
        }).AddMvcCore();

        PluginManager.Load(mvcCoreBuilder, configuration, hostEnvironment.Object, _fixture.CreateContext);
    }

    private string ShadowFolder(string folder) => Path.Combine(_fixture.PluginsFolder, "bin", folder);

    private static void WriteDepsJson(string folder, string assemblyName)
    {
        File.WriteAllText(Path.Combine(folder, assemblyName + ".deps.json"), $$"""
            {
              "runtimeTarget": { "name": ".NETCoreApp,Version=v10.0" },
              "targets": { ".NETCoreApp,Version=v10.0": { "{{assemblyName}}/1.0.0": { "runtime": { "{{assemblyName}}.dll": {} } } } },
              "libraries": { "{{assemblyName}}/1.0.0": { "type": "project", "serviceable": false, "sha512": "" } }
            }
            """);
    }

    private static PluginInfo Plugin(string systemName) =>
        PluginManager.ReferencedPlugins.Single(x => x.SystemName == systemName);

    private static bool IsLoaded(string assemblyName) =>
        AssemblyLoadContext.All.SelectMany(x => x.Assemblies).Any(x => x.GetName().Name == assemblyName);

    private static List<string> MappedTypeNames()
    {
        var entities = typeof(BsonMapper).GetField("_entities", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return ((IDictionary)entities.GetValue(BsonMapper.Global)!).Keys.Cast<Type>().Select(x => x.Name).ToList();
    }
}
