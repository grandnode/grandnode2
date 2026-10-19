using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Tests.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.TypeSearcher;

[TestClass]
public class TypeSearcherTests
{
    private readonly TypeSearch.TypeSearcher _typeSearcher = new();

    [TestMethod]
    public void GetAssembliesTest()
    {
        Assert.IsTrue(_typeSearcher.GetAssemblies().Any());
    }

    [TestMethod]
    public void ClassesOfTypeTest_onlyConcreteClasses_false()
    {
        Assert.IsTrue(_typeSearcher.ClassesOfType<ICacheBase>().Any());
    }

    [TestMethod]
    public void ClassesOfTypeTest_onlyConcreteClasses_true()
    {
        Assert.IsFalse(_typeSearcher.ClassesOfType<IWorkContext>().Any());
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetAssemblies_ExcludesUninstalledPluginAssembly()
    {
        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [SamplePluginInfo(false)];

            Assert.DoesNotContain(typeof(SampleBasePlugin).Assembly, _typeSearcher.GetAssemblies());
            Assert.DoesNotContain(typeof(SampleBasePlugin), _typeSearcher.ClassesOfType<IPlugin>());
            Assert.IsTrue(_typeSearcher.ClassesOfType<ICacheBase>().Any());
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetAssemblies_KeepsInstalledPluginAssembly()
    {
        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [SamplePluginInfo(true)];

            Assert.Contains(typeof(SampleBasePlugin).Assembly, _typeSearcher.GetAssemblies());
            Assert.Contains(typeof(SampleBasePlugin), _typeSearcher.ClassesOfType<IPlugin>());
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }
    }

    private static PluginInfo SamplePluginInfo(bool installed)
    {
        return new PluginInfo(null, typeof(SampleBasePlugin).Assembly, typeof(SampleBasePlugin)) {
            SystemName = "SamplePlugin",
            Installed = installed
        };
    }
}