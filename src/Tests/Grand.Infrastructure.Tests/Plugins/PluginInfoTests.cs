using Grand.Infrastructure.Plugins;
using Grand.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
public class PluginInfoTests
{
    public interface IMissingDependency;

    public class NeedyPlugin(IMissingDependency dependency) : BasePlugin
    {
        public IMissingDependency Dependency { get; } = dependency;
    }

    [TestMethod]
    public void Instance_DependencyMissing_KeepsInnerException()
    {
        var services = new ServiceCollection();
        services.AddTransient<NeedyPlugin>();
        using var provider = services.BuildServiceProvider();
        var info = new PluginInfo { SystemName = "Needy", PluginType = typeof(NeedyPlugin) };

        var ex = Assert.Throws<GrandException>(() => info.Instance(provider));

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<InvalidOperationException>(ex.InnerException);
        StringAssert.Contains(ex.Message, "Needy");
    }

    [TestMethod]
    public void Instance_NoPluginType_ThrowsGrandException()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var info = new PluginInfo { SystemName = "Empty" };

        var ex = Assert.Throws<GrandException>(() => info.Instance(provider));

        Assert.AreEqual("Plugin Empty has no IPlugin class", ex.Message);
    }
}
