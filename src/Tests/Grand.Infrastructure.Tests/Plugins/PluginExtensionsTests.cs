using Grand.Infrastructure.Plugins;
using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestContext = NUnit.Framework.TestContext;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
public class PluginExtensionsTests
{
    private readonly SampleBasePlugin sampleBasePlugin;

    public PluginExtensionsTests()
    {
        CommonPath.BaseDirectory = TestContext.CurrentContext.TestDirectory;
        sampleBasePlugin = new SampleBasePlugin();
    }

    [TestMethod]
    public async Task ParseInstalledPluginsFileTest()
    {
        await sampleBasePlugin.Install();
        var plugins = PluginExtensions.ParseInstalledPluginsFile(CommonPath.InstalledPluginsFilePath);
        Assert.IsNotNull(plugins);
    }

    [TestMethod]
    public async Task SaveInstalledPluginsFileTest()
    {
        await PluginExtensions.SaveInstalledPluginsFile(new List<string> { "plugin1", "plugin2" },
            CommonPath.InstalledPluginsFilePath);
        var plugins = PluginExtensions.ParseInstalledPluginsFile(CommonPath.InstalledPluginsFilePath);
        Assert.AreEqual(2, plugins.Count);
    }
}