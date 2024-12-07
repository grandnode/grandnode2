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
        Assert.AreEqual(1, plugins.Count);
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
        Assert.AreEqual(1, plugins.Count);
    }
}