using Grand.Data;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Tests.Plugins.Fixtures;
using Grand.SharedKernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text.Json;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
[DoNotParallelize]
public class PluginStateTests
{
    private PluginStateStoreFixture _stateStore;
    private string _installedFile;
    private bool _recordsPluginState;

    [TestInitialize]
    public void Init()
    {
        _recordsPluginState = PluginManager.RecordsPluginState;
        //as the storefront
        PluginManager.RecordsPluginState = true;
        _stateStore = new PluginStateStoreFixture();
        _installedFile = Path.Combine(Path.GetTempPath(), "grand-plugin-state-tests", Guid.NewGuid().ToString("N"),
            "InstalledPlugins.cfg");
        Directory.CreateDirectory(Path.GetDirectoryName(_installedFile)!);
    }

    [TestCleanup]
    public void Cleanup()
    {
        PluginManager.RecordsPluginState = _recordsPluginState;
        _stateStore.Dispose();
        Directory.Delete(Path.GetDirectoryName(_installedFile)!, true);
    }

    [TestMethod]
    public void ReadInstalledPlugins_FileAndEmptyDatabase_TakesTheFileOverAndRenamesIt()
    {
        WriteFile("Plugin.A", "Plugin.B");

        var installed = PluginManager.ReadInstalledPlugins(_stateStore.Store, _installedFile, null);

        CollectionAssert.AreEquivalent(new[] { "Plugin.A", "Plugin.B" }, installed);
        CollectionAssert.AreEquivalent(new[] { "Plugin.A", "Plugin.B" }, _stateStore.Installed());
        Assert.IsFalse(File.Exists(_installedFile));
        Assert.IsTrue(File.Exists(_installedFile + PluginManager.TakenOverFileSuffix));
    }

    [TestMethod]
    public void ReadInstalledPlugins_FileWithBlankEntries_TakesTheOthersOver()
    {
        File.WriteAllText(_installedFile, """["Plugin.A", "", null, " "]""");

        var installed = PluginManager.ReadInstalledPlugins(_stateStore.Store, _installedFile, null);

        CollectionAssert.AreEqual(new[] { "Plugin.A" }, installed);
    }

    [TestMethod]
    public void ReadInstalledPlugins_FileTakenOverByAnotherInstanceMeanwhile_ReadsTheDatabase()
    {
        WriteFile("Plugin.A");
        var store = new Mock<IPluginStateStore>();
        store.Setup(x => x.HasDatabase).Returns(true);
        //the other instance renames the file between the check that it exists and the read
        store.Setup(x => x.ImportInstalled(It.IsAny<Func<IEnumerable<string>>>()))
            .Returns((Func<IEnumerable<string>> systemNames) =>
            {
                File.Move(_installedFile, _installedFile + PluginManager.TakenOverFileSuffix);
                return systemNames().Any();
            });
        store.Setup(x => x.InstalledSystemNames()).Returns(["Plugin.A"]);

        var installed = PluginManager.ReadInstalledPlugins(store.Object, _installedFile, null);

        CollectionAssert.AreEqual(new[] { "Plugin.A" }, installed);
    }

    [TestMethod]
    public async Task ReadInstalledPlugins_FileAndDatabaseListingPlugins_IgnoresTheFileAndRenamesIt()
    {
        //an uninstalled plugin counts: the database has the list
        await _stateStore.Store.SetInstalled("Plugin.A", false);
        await _stateStore.Store.SetInstalled("Plugin.C", true);
        WriteFile("Plugin.A", "Plugin.B");

        var installed = PluginManager.ReadInstalledPlugins(_stateStore.Store, _installedFile, null);

        CollectionAssert.AreEqual(new[] { "Plugin.C" }, installed);
        Assert.IsFalse(File.Exists(_installedFile));
        Assert.IsTrue(File.Exists(_installedFile + PluginManager.TakenOverFileSuffix));
    }

    [TestMethod]
    public void ReadInstalledPlugins_NoFile_ReadsTheDatabase()
    {
        _stateStore.Store.ImportInstalled(() => ["Plugin.A"]);

        var installed = PluginManager.ReadInstalledPlugins(_stateStore.Store, _installedFile, null);

        CollectionAssert.AreEqual(new[] { "Plugin.A" }, installed);
    }

    [TestMethod]
    public void ReadInstalledPlugins_ApplicationNotInstalled_NoneInstalledAndFileKept()
    {
        var store = new Mock<IPluginStateStore>();
        store.Setup(x => x.HasDatabase).Returns(false);
        WriteFile("Plugin.A");

        var installed = PluginManager.ReadInstalledPlugins(store.Object, _installedFile, null);

        Assert.IsEmpty(installed);
        Assert.IsTrue(File.Exists(_installedFile));
        store.Verify(x => x.InstalledSystemNames(), Times.Never);
        store.Verify(x => x.ImportInstalled(It.IsAny<Func<IEnumerable<string>>>()), Times.Never);
    }

    [TestMethod]
    public void ReadInstalledPlugins_DatabaseCannotBeRead_Throws()
    {
        var store = new Mock<IPluginStateStore>();
        store.Setup(x => x.HasDatabase).Returns(true);
        store.Setup(x => x.InstalledSystemNames()).Throws(new TimeoutException("no server"));

        var ex = Assert.Throws<GrandException>(() =>
            PluginManager.ReadInstalledPlugins(store.Object, _installedFile, null));

        StringAssert.Contains(ex.Message, "no server");
    }

    [TestMethod]
    public async Task ReadInstalledPlugins_InvalidFileAndDatabaseListingPlugins_IgnoresTheFileAndRenamesIt()
    {
        await _stateStore.Store.SetInstalled("Plugin.C", true);
        File.WriteAllText(_installedFile, "{ not json");

        var installed = PluginManager.ReadInstalledPlugins(_stateStore.Store, _installedFile, null);

        CollectionAssert.AreEqual(new[] { "Plugin.C" }, installed);
        Assert.IsFalse(File.Exists(_installedFile));
    }

    [TestMethod]
    public void ReadInstalledPlugins_FileThatCannotBeRenamed_StillStarts()
    {
        WriteFile("Plugin.A");
        var logs = new CapturingLoggerProvider();

        List<string> installed;
        //an open handle without delete sharing keeps Windows from renaming the file
        using (new FileStream(_installedFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            installed = PluginManager.ReadInstalledPlugins(_stateStore.Store, _installedFile,
                logs.CreateLogger("PluginManager"));

        CollectionAssert.AreEqual(new[] { "Plugin.A" }, installed);
        if (OperatingSystem.IsWindows())
            Assert.IsTrue(logs.Entries.Any(x => x.Message.Contains("could not be renamed")));
    }

    [TestMethod]
    public void ReadInstalledPlugins_NoFileToTakeOver_ReadsTheDatabaseOnly()
    {
        WriteFile("Plugin.A");

        var installed = PluginManager.ReadInstalledPlugins(_stateStore.Store, null, null);

        Assert.IsEmpty(installed);
        Assert.IsTrue(File.Exists(_installedFile));
    }

    [TestMethod]
    public void ReadInstalledPlugins_InvalidFile_ThrowsAndKeepsTheFile()
    {
        File.WriteAllText(_installedFile, "{ not json");

        Assert.Throws<GrandException>(() =>
            PluginManager.ReadInstalledPlugins(_stateStore.Store, _installedFile, null));

        Assert.IsTrue(File.Exists(_installedFile));
        Assert.IsEmpty(_stateStore.Installed());
    }

    [TestMethod]
    public async Task SaveLoadResults_RecordsEveryPluginAndKeepsWhetherItIsInstalled()
    {
        await _stateStore.Store.SetInstalled("Plugin.Working", true);
        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [
                new PluginInfo {
                    SystemName = "Plugin.Working", FriendlyName = "Working", Version = "1.0",
                    SupportedVersion = GrandVersion.SupportedPluginVersion, Installed = true
                },
                new PluginInfo {
                    SystemName = "Plugin.Old", FriendlyName = "Old", Version = "0.9", SupportedVersion = "1.0"
                },
                .. PluginLoader.DescribeProblem(new PluginLoadProblem("Broken", "Could not load Broken.dll"), [])
            ];

            PluginManager.SaveLoadResults();
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }

        var working = _stateStore.Document("Plugin.Working");
        Assert.IsTrue(working["Installed"].AsBoolean);
        Assert.AreEqual("Working", working["FriendlyName"].AsString);
        Assert.IsTrue(working["LoadError"].IsNull);

        var old = _stateStore.Document("Plugin.Old");
        Assert.IsFalse(old["Installed"].AsBoolean);
        StringAssert.Contains(old["LoadError"].AsString, "Built for GrandNode 1.0");

        var broken = _stateStore.Document("Broken");
        Assert.IsFalse(broken["Installed"].AsBoolean);
        Assert.AreEqual("Broken", broken["FolderName"].AsString);
        Assert.AreEqual("Could not load Broken.dll", broken["LoadError"].AsString);
    }

    [TestMethod]
    public async Task SaveLoadResults_InstalledPluginThatFailed_StaysInstalled()
    {
        await _stateStore.Store.SetInstalled("Plugin.Broken", true);
        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = PluginLoader.DescribeProblem(
                new PluginLoadProblem("Broken", "Could not load Broken.dll", ["Plugin.Broken"]), ["Plugin.Broken"]);

            PluginManager.SaveLoadResults();
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }

        var broken = _stateStore.Document("Plugin.Broken");
        Assert.IsTrue(broken["Installed"].AsBoolean);
        Assert.AreEqual("Could not load Broken.dll", broken["LoadError"].AsString);
        CollectionAssert.AreEqual(new[] { "Plugin.Broken" }, _stateStore.Installed());
    }

    [TestMethod]
    public void SaveLoadResults_OtherThanTheStorefront_RecordsNothing()
    {
        PluginManager.RecordsPluginState = false;
        var referencedPlugins = PluginManager.ReferencedPlugins;
        try
        {
            PluginManager.ReferencedPlugins = [
                new PluginInfo { SystemName = "Plugin.Working", SupportedVersion = GrandVersion.SupportedPluginVersion }
            ];

            PluginManager.SaveLoadResults();
        }
        finally
        {
            PluginManager.ReferencedPlugins = referencedPlugins;
        }

        Assert.IsNull(_stateStore.Document("Plugin.Working"));
    }

    [TestMethod]
    public async Task ClearPlugins_MarksNoneInstalled()
    {
        await _stateStore.Store.SetInstalled("Plugin.A", true);
        //initialized once per process: ClearPlugins also deletes the file of an older version, wherever it points
        PluginPaths.Initialize(_installedFile);

        await PluginManager.ClearPlugins();

        Assert.IsEmpty(_stateStore.Installed());
    }

    private void WriteFile(params string[] systemNames)
    {
        File.WriteAllText(_installedFile, JsonSerializer.Serialize(systemNames));
    }
}
