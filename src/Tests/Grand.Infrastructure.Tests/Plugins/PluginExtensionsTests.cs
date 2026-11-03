using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Tests.Plugins.Fixtures;
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
    public async Task MarkPluginAsUninstalled_DifferentCase_Removes()
    {
        await PluginExtensions.MarkPluginAsInstalled("Plugin1");
        await PluginExtensions.MarkPluginAsUninstalled("plugin1");
        var plugins = PluginExtensions.ParseInstalledPluginsFile(PluginPaths.Instance.InstalledPluginsFile);
        Assert.IsEmpty(plugins);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task MarkPluginAsInstalled_Concurrent_KeepsBoth()
    {
        var names = Enumerable.Range(0, 20).Select(i => $"plugin{i}").ToList();
        await Task.WhenAll(names.Select(n => Task.Run(() => PluginExtensions.MarkPluginAsInstalled(n))));
        var plugins = PluginExtensions.ParseInstalledPluginsFile(PluginPaths.Instance.InstalledPluginsFile);
        Assert.HasCount(20, plugins);
        CollectionAssert.AreEquivalent(names, plugins.ToList());
    }

    [TestMethod]
    [DoNotParallelize]
    public void ParseInstalledPluginsFile_InvalidJson_ThrowsWithPath()
    {
        var path = PluginPaths.Instance.InstalledPluginsFile;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ not json");
        try
        {
            var ex = Assert.Throws<Grand.SharedKernel.GrandException>(() =>
                PluginExtensions.ParseInstalledPluginsFile(path));
            StringAssert.Contains(ex.Message, path);
            StringAssert.Contains(ex.Message, "is not a valid installed-plugins file");
        }
        finally
        {
            File.Delete(path);
        }
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

    [TestMethod]
    [DoNotParallelize]
    public void OnlyInstalledPlugins_SatelliteOfUninstalledPlugin_ReturnsFalse()
    {
        using var fixture = new PluginFolderFixture();
        fixture.AddPlugin("Sat", "Ext.Sat", "Ext.Sat", "public class SatPlugin : BasePlugin { }");
        TestPluginBuilder.Compile(fixture.Folder("Sat"), "Ext.Sat.Core", "public class Core { }");
        var plugin = fixture.Load("Sat", false);

        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [plugin];
            Assert.IsFalse(PluginExtensions.OnlyInstalledPlugins(fixture.Loaded("Ext.Sat.Core")));
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void OnlyInstalledPlugins_InstalledPluginWithLoadError_ReturnsFalse()
    {
        using var fixture = new PluginFolderFixture();
        var missing = fixture.CompileElsewhere("Ext.MissingDep", "namespace Missing { public class Base { } }");
        fixture.AddPlugin("Broken", "Ext.Broken", "Ext.Broken", """
            public class Derived : Missing.Base { }
            public class BrokenPlugin : BasePlugin { }
            """, missing);
        TestPluginBuilder.Compile(fixture.Folder("Broken"), "Ext.Broken.Core", "public class Core { }");
        var plugin = fixture.Load("Broken", true);
        Assert.IsNotNull(plugin.LoadError);

        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [plugin];
            Assert.IsFalse(PluginExtensions.OnlyInstalledPlugins(fixture.Loaded("Ext.Broken.Core")));
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void OnlyInstalledPlugins_DependencySharedWithInstalledPlugin_ReturnsTrue()
    {
        using var fixture = new PluginFolderFixture();
        fixture.AddPlugin("A", "Ext.A", "Ext.A", "public class APlugin : BasePlugin { }");
        fixture.AddPlugin("B", "Ext.B", "Ext.B", "public class BPlugin : BasePlugin { }");
        TestPluginBuilder.Compile(fixture.Folder("A"), "Ext.Shared", "public class Shared { }");
        TestPluginBuilder.Compile(fixture.Folder("B"), "Ext.Shared", "public class Shared { }");
        var uninstalled = fixture.Load("A", false);
        var installed = fixture.Load("B", true);

        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [uninstalled, installed];
            Assert.IsTrue(PluginExtensions.OnlyInstalledPlugins(fixture.Loaded("Ext.Shared")));
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