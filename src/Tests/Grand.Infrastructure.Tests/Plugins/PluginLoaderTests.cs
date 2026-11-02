using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Tests.Plugins.Fixtures;
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
