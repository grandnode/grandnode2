using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Tests.Plugins.Fixtures;
using Grand.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
public class PluginLoaderTests
{
    private PluginFolderFixture _fixture;

    [TestInitialize]
    public void Init()
    {
        _fixture = new PluginFolderFixture();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture.Dispose();
    }

    [TestMethod]
    public void Load_MissingDependency_SetsLoadErrorAndDoesNotThrow()
    {
        var missing = _fixture.CompileElsewhere("Loader.MissingDep", "namespace Missing { public class Base { } }");
        _fixture.AddPlugin("P", "Loader.Missing", "Loader.Missing", """
            public class Derived : Missing.Base { }
            public class MissingPlugin : BasePlugin { }
            """, missing);

        var plugin = _fixture.Load("P", true);

        Assert.IsNotNull(plugin.LoadError);
        Assert.Contains("Loader.MissingDep", plugin.LoadError);
        Assert.IsNull(plugin.PluginType);
        Assert.IsFalse(plugin.IsActive);
        Assert.AreEqual("Loader.Missing", plugin.SystemName);
    }

    [TestMethod]
    public void Load_AbstractPluginBase_PicksConcreteTypeAndBuildsProvider()
    {
        _fixture.AddPlugin("P", "Loader.Abstract", "Loader.Abstract", """
            public interface IAbstractPlugin : IPlugin { }
            public abstract class AbstractPluginBase : BasePlugin, IAbstractPlugin { }
            public class ConcretePlugin : AbstractPluginBase { }
            """);

        var plugin = _fixture.Load("P", true);

        Assert.IsNull(plugin.LoadError);
        Assert.AreEqual("ConcretePlugin", plugin.PluginType.Name);
        Assert.IsTrue(plugin.IsActive);

        //PluginManager registers the plugin type and nothing else
        var services = new ServiceCollection();
        services.AddScoped(plugin.PluginType);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        Assert.IsInstanceOfType<IPlugin>(scope.ServiceProvider.GetRequiredService(plugin.PluginType));
    }

    [TestMethod]
    public void Load_NoIPluginClass_SetsLoadError()
    {
        _fixture.AddPlugin("P", "Loader.NoPlugin", "Loader.NoPlugin", "public class NotAPlugin { }");

        var plugin = _fixture.Load("P", true);

        Assert.AreEqual("has no IPlugin class", plugin.LoadError);
        Assert.IsNull(plugin.PluginType);
    }

    [TestMethod]
    public void Load_DependencyKeptByIncompatiblePlugin_StillResolves()
    {
        const string sharedSource = "namespace Shared { public class Base { } }";
        var shared = TestPluginBuilder.Compile(_fixture.Folder("A"), "Loader.Shared", sharedSource,
            new Version(1, 0, 0, 0));
        _fixture.AddPlugin("A", "Loader.A", "Loader.A", """
            public class Derived : Shared.Base { }
            public class APlugin : BasePlugin { }
            """, shared);
        //a plugin built for another version ships the higher copy; it is never loaded, so it must not take the dependency
        TestPluginBuilder.Compile(_fixture.Folder("B"), "Loader.Shared", sharedSource, new Version(2, 0, 0, 0));
        TestPluginBuilder.Compile(_fixture.Folder("B"), "Loader.B", """
            using Grand.Infrastructure.Plugins;
            [assembly: PluginInfo(SystemName = "Loader.B", SupportedVersion = "1.0")]
            """, null, PluginFolderFixture.CoreAssemblyPath);

        var plugin = _fixture.Load("A", true);

        Assert.IsNull(plugin.LoadError);
        Assert.AreEqual("APlugin", plugin.PluginType.Name);
    }

    [TestMethod]
    public void Problem_KnownSystemNameOfInstalledPlugin_IsInstalledAndFailsStrictMode()
    {
        _fixture.AddPlugin("Multi", "Loader.One", "Loader.One");
        _fixture.AddPlugin("Multi", "Loader.Two", "Loader.Two");
        var problems = _fixture.Scan().Problems;

        var problem = problems.Single();
        CollectionAssert.AreEquivalent(new[] { "Loader.One", "Loader.Two" }, problem.SystemNames.ToList());
        var entries = PluginLoader.DescribeProblem(problem, ["loader.one"]).ToList();

        var one = entries.Single(x => x.SystemName == "Loader.One");
        Assert.IsTrue(one.Installed);
        Assert.AreEqual(problem.Message, one.LoadError);
        Assert.IsFalse(entries.Single(x => x.SystemName == "Loader.Two").Installed);

        var ex = Assert.ThrowsExactly<GrandException>(() => PluginLoader.ThrowOnLoadError(entries, problems, true));
        Assert.Contains("Loader.One", ex.Message);
        PluginLoader.ThrowOnLoadError(entries, problems, false);
    }

    [TestMethod]
    public void Problem_UnknownSystemName_FailsStrictModeOnly()
    {
        _fixture.AddPlugin("Nameless", "Loader.Nameless", "");
        var problems = _fixture.Scan().Problems;

        var problem = problems.Single();
        Assert.IsEmpty(problem.SystemNames);
        var entries = PluginLoader.DescribeProblem(problem, []).ToList();
        Assert.AreEqual("Nameless", entries.Single().SystemName);
        Assert.IsFalse(entries.Single().Installed);

        var ex = Assert.ThrowsExactly<GrandException>(() => PluginLoader.ThrowOnLoadError(entries, problems, true));
        Assert.Contains("Nameless", ex.Message);
        Assert.Contains("has no SystemName", ex.Message);
        PluginLoader.ThrowOnLoadError(entries, problems, false);
    }

    [TestMethod]
    public void Load_SatelliteAssembly_IsInAssemblyNames()
    {
        _fixture.AddPlugin("P", "Loader.Sat", "Loader.Sat", "public class SatPlugin : BasePlugin { }");
        TestPluginBuilder.Compile(_fixture.Folder("P"), "Loader.Sat.Core", "public class Core { }");

        var plugin = _fixture.Load("P", false);

        Assert.IsNull(plugin.LoadError);
        Assert.Contains("Loader.Sat", plugin.AssemblyNames);
        Assert.Contains("Loader.Sat.Core", plugin.AssemblyNames);
    }
}
