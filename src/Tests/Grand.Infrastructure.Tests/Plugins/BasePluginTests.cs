using Grand.Infrastructure.Tests.Plugins.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
[DoNotParallelize]
public class BasePluginTests
{
    private PluginStateStoreFixture _stateStore;
    private SampleBasePlugin sampleBasePlugin;

    [TestInitialize]
    public void Init()
    {
        _stateStore = new PluginStateStoreFixture();
        sampleBasePlugin = new SampleBasePlugin();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _stateStore.Dispose();
    }

    [TestMethod]
    public void ConfigurationUrlTest()
    {
        Assert.IsNotNull(sampleBasePlugin.ConfigurationUrl());
    }

    [TestMethod]
    public async Task InstallTest()
    {
        await sampleBasePlugin.Install();
        var plugins = _stateStore.Installed();
        Assert.AreEqual("SamplePlugin", plugins.FirstOrDefault());
    }

    [TestMethod]
    public async Task UninstallTest_WithInstall()
    {
        await sampleBasePlugin.Install();
        await sampleBasePlugin.Uninstall();
        Assert.IsEmpty(_stateStore.Installed());
        Assert.IsFalse(_stateStore.Document("SamplePlugin")["Installed"].AsBoolean);
    }

    [TestMethod]
    public async Task UninstallTest()
    {
        await sampleBasePlugin.Uninstall();
        Assert.IsEmpty(_stateStore.Installed());
    }
}
