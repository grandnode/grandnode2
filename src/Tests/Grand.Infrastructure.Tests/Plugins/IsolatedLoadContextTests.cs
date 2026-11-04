using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Tests.Plugins.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
[DoNotParallelize]
public class IsolatedLoadContextTests
{
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
    public void TwoPlugins_DifferentDependencyVersions_BothRun()
    {
        var one = TestPluginBuilder.Compile(_fixture.Folder("One"), "Isolated.SharedDep", """
            namespace SharedDep { public static class Dep { public static string Value() => "1.0"; } }
            """, new Version(1, 0, 0, 0));
        _fixture.AddPlugin("One", "Isolated.One", "Isolated.One", """
            public class OnePlugin : BasePlugin { }
            public static class Entry { public static string Run() => SharedDep.Dep.Value(); }
            """, one);
        var two = TestPluginBuilder.Compile(_fixture.Folder("Two"), "Isolated.SharedDep", """
            namespace SharedDep
            {
                public static class Dep
                {
                    public static string Value() => "2.0";
                    public static string OnlyInTwo() => "2.0 only";
                }
            }
            """, new Version(2, 0, 0, 0));
        _fixture.AddPlugin("Two", "Isolated.Two", "Isolated.Two", """
            public class TwoPlugin : BasePlugin { }
            public static class Entry { public static string Run() => SharedDep.Dep.OnlyInTwo(); }
            """, two);

        Load("Isolated.One,Isolated.Two");

        Assert.AreEqual("1.0", Run(Plugin("Isolated.One")));
        Assert.AreEqual("2.0 only", Run(Plugin("Isolated.Two")));
    }

    [TestMethod]
    public void Load_HostAssemblyName_ComesFromDefaultContext()
    {
        _fixture.AddPlugin("Host", "Isolated.Host", "Isolated.Host", "public class HostPlugin : BasePlugin { }");
        File.Copy(PluginFolderFixture.CoreAssemblyPath, Path.Combine(_fixture.Folder("Host"), "Grand.Infrastructure.dll"));

        Load("Isolated.Host");

        var plugin = Plugin("Isolated.Host");
        Assert.IsNull(plugin.LoadError);
        var instance = Activator.CreateInstance(plugin.PluginType);
        Assert.IsInstanceOfType<IPlugin>(instance);
        Assert.AreSame(typeof(IPlugin).Assembly,
            plugin.PluginType.GetInterfaces().Single(x => x.Name == nameof(IPlugin)).Assembly);
    }

    [TestMethod]
    public void PluginTypes_AreRecognisedByMvcApplicationPart()
    {
        //no Controller suffix: it is recognised only through the host's [Controller] on ControllerBase
        _fixture.AddPlugin("Mvc", "Isolated.Mvc", "Isolated.Mvc", """
            public class MvcPlugin : BasePlugin { }
            public class PluginEndpoint : Microsoft.AspNetCore.Mvc.ControllerBase { }
            """);

        var mvcCoreBuilder = Load("Isolated.Mvc");

        var feature = new ControllerFeature();
        mvcCoreBuilder.PartManager.PopulateFeature(feature);
        Assert.IsTrue(feature.Controllers.Any(x =>
            x.Name == "PluginEndpoint" && x.Assembly == Plugin("Isolated.Mvc").ReferencedAssembly));
    }

    [TestMethod]
    public void Load_DependencyListedInDepsJson_IsResolvedFromItsListedPath()
    {
        //outside the top of the folder: only the plugin's deps.json says where it is, listed the way a package's
        //runtimes/<rid> asset is (Payments.StripeCheckout ships one)
        var lib = TestPluginBuilder.Compile(Path.Combine(_fixture.Folder("Deps"), "lib"), "Isolated.Deps.Lib",
            "namespace DepsLib { public static class Lib { public static string Value() => \"from lib\"; } }");
        _fixture.AddPlugin("Deps", "Isolated.Deps", "Isolated.Deps", """
            public class DepsPlugin : BasePlugin { }
            public static class Entry { public static string Run() => DepsLib.Lib.Value(); }
            """, lib);
        File.WriteAllText(Path.Combine(_fixture.Folder("Deps"), "Isolated.Deps.deps.json"), """
            {
              "runtimeTarget": { "name": ".NETCoreApp,Version=v10.0" },
              "targets": {
                ".NETCoreApp,Version=v10.0": {
                  "Isolated.Deps/1.0.0": {
                    "dependencies": { "Isolated.Deps.Lib": "1.0.0" },
                    "runtime": { "Isolated.Deps.dll": {} }
                  },
                  "Isolated.Deps.Lib/1.0.0": {
                    "runtimeTargets": {
                      "lib/Isolated.Deps.Lib.dll": { "rid": "any", "assetType": "runtime", "assemblyVersion": "1.0.0.0", "fileVersion": "1.0.0.0" }
                    }
                  }
                }
              },
              "libraries": {
                "Isolated.Deps/1.0.0": { "type": "project", "serviceable": false, "sha512": "" },
                "Isolated.Deps.Lib/1.0.0": { "type": "project", "serviceable": false, "sha512": "" }
              }
            }
            """);

        Load("Isolated.Deps");

        var plugin = Plugin("Isolated.Deps");
        Assert.AreEqual("from lib", Run(plugin));
        Assert.AreEqual(Path.GetFullPath(lib), PluginFolderFixture.Loaded(plugin, "Isolated.Deps.Lib").Location);
    }

    [TestMethod]
    public void Load_AssemblyOfAnotherPlugin_IsLoadErrorNotCrash()
    {
        //not supported: a plugin takes nothing from another plugin's context
        var api = _fixture.AddPlugin("Api", "Isolated.Api", "Isolated.Api", """
            public class ApiPlugin : BasePlugin { }
            namespace ApiTypes { public class Api { } }
            """);
        _fixture.AddPlugin("User", "Isolated.User", "Isolated.User", """
            public class UserPlugin : BasePlugin { }
            public class UsesApi : ApiTypes.Api { }
            """, api);

        Load("Isolated.Api,Isolated.User");

        Assert.IsTrue(Plugin("Isolated.Api").IsActive);
        var user = Plugin("Isolated.User");
        Assert.IsNotNull(user.LoadError);
        Assert.Contains("Isolated.Api", user.LoadError);
        Assert.IsNull(user.ReferencedAssembly);
    }

    [TestMethod]
    public void FindPlugin_AssemblyInPluginsContext_FindsThePlugin()
    {
        _fixture.AddPlugin("Find", "Isolated.Find", "Isolated.Find", "public class FindPlugin : BasePlugin { }");
        TestPluginBuilder.Compile(_fixture.Folder("Find"), "Isolated.Find.Core", "public class Core { }");

        Load("Isolated.Find");

        var plugin = Plugin("Isolated.Find");
        Assert.AreSame(plugin, PluginManager.FindPlugin(plugin.PluginType));
        Assert.AreSame(plugin, PluginManager.FindPlugin(PluginFolderFixture.Loaded(plugin, "Isolated.Find.Core")));
        Assert.IsNull(PluginManager.FindPlugin(typeof(IsolatedLoadContextTests)));
    }

    private IMvcCoreBuilder Load(string installed)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> {
                ["Extensions:InstalledPlugins"] = installed
            })
            .Build();
        var hostEnvironment = new Mock<IWebHostEnvironment>();
        hostEnvironment.Setup(x => x.ContentRootPath).Returns(Path.GetDirectoryName(_fixture.PluginsFolder)!);
        var mvcCoreBuilder = new ServiceCollection().AddLogging().AddMvcCore();

        PluginManager.Load(mvcCoreBuilder, configuration, hostEnvironment.Object, _fixture.CreateContext);
        return mvcCoreBuilder;
    }

    private static PluginInfo Plugin(string systemName) =>
        PluginManager.ReferencedPlugins.Single(x => x.SystemName == systemName);

    private static string Run(PluginInfo plugin)
    {
        Assert.IsNull(plugin.LoadError);
        return (string)plugin.ReferencedAssembly.GetType("Entry", true)!.GetMethod("Run")!.Invoke(null, null);
    }
}
