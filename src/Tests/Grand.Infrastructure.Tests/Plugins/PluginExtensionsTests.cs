using Grand.Infrastructure.Plugins;
using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestContext = NUnit.Framework.TestContext;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
public class PluginExtensionsTests
{
    public PluginExtensionsTests()
    {
        
        var pluginPaths = Path.Combine(TestContext.CurrentContext.TestDirectory, CommonPath.AppData, CommonPath.InstalledPluginsFile);
        PluginPaths.Initialize(pluginPaths);
        PluginManager.ClearPlugins();
    }

    [TestMethod]
    public async Task ParseInstalledPluginsFileTest()
    {
        var sampleBasePlugin = new SampleBasePlugin();
        await sampleBasePlugin.Install();
        var plugins = PluginExtensions.ParseInstalledPluginsFile(PluginPaths.Instance.InstalledPluginsFile);
        Assert.IsNotNull(plugins);
    }

    [TestMethod]
    public async Task MarkPluginAsInstalledTest()
    {
        //Act
        await PluginExtensions.MarkPluginAsInstalled("plugin1");
        //Assert
        var plugins = PluginExtensions.ParseInstalledPluginsFile(PluginPaths.Instance.InstalledPluginsFile);
        Assert.HasCount(1, plugins);
    }

    [TestMethod]
    public async Task MarkPluginAsUninstalled()
    {
        //Arrange
        await PluginExtensions.MarkPluginAsInstalled("plugin1");
        await PluginExtensions.MarkPluginAsInstalled("plugin2");
        //Act
        await PluginExtensions.MarkPluginAsUninstalled("plugin1");
        //Assert
        var plugins = PluginExtensions.ParseInstalledPluginsFile(PluginPaths.Instance.InstalledPluginsFile);
        Assert.HasCount(1, plugins);
    }

    [TestMethod]
    [DoNotParallelize]
    public void OnlyInstalledPlugins_Assembly_NotAPlugin_ReturnsTrue()
    {
        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [];
            Assert.IsTrue(PluginExtensions.OnlyInstalledPlugins(typeof(SampleBasePlugin).Assembly));
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void OnlyInstalledPlugins_Assembly_UninstalledPlugin_ReturnsFalse()
    {
        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [SamplePluginInfo(false)];
            Assert.IsFalse(PluginExtensions.OnlyInstalledPlugins(typeof(SampleBasePlugin).Assembly));
            Assert.IsFalse(PluginExtensions.OnlyInstalledPlugins(typeof(SampleBasePlugin)));
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void OnlyInstalledPlugins_Assembly_InstalledPlugin_ReturnsTrue()
    {
        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [SamplePluginInfo(true)];
            Assert.IsTrue(PluginExtensions.OnlyInstalledPlugins(typeof(SampleBasePlugin).Assembly));
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