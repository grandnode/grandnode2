using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Tests.Plugins.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
public class PluginPendingChangesTests
{
    private PluginFolderFixture _fixture;
    private string _installedFile;

    [TestInitialize]
    public void Init()
    {
        _fixture = new PluginFolderFixture();
        _installedFile = Path.Combine(Path.GetDirectoryName(_fixture.PluginsFolder)!, "App_Data",
            "InstalledPlugins.cfg");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture.Dispose();
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetPendingChanges_NothingChanged_IsEmpty()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        _fixture.AddPlugin("B", "Pending.B", "Pending.B");
        WriteInstalled("Pending.A");

        var changes = PendingAfter(() => { });

        Assert.IsEmpty(changes);
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetPendingChanges_MarkedInstalledAfterLoad_ReportsInstalled()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        WriteInstalled();

        var changes = PendingAfter(() => WriteInstalled("Pending.A"));

        Assert.AreEqual(new PluginPendingChange("Pending.A", "A", PluginPendingChangeKind.Installed),
            changes.Single());
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetPendingChanges_MarkedUninstalledAfterLoad_ReportsUninstalled()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        WriteInstalled("Pending.A");

        var changes = PendingAfter(() => WriteInstalled());

        Assert.AreEqual(new PluginPendingChange("Pending.A", "A", PluginPendingChangeKind.Uninstalled),
            changes.Single());
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetPendingChanges_NewPluginFolder_ReportsAdded()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        WriteInstalled();

        var changes = PendingAfter(() =>
        {
            _fixture.AddPlugin("B", "Pending.B", "Pending.B");
            //a folder without a plugin assembly is not a plugin
            Directory.CreateDirectory(_fixture.Folder("NotAPlugin"));
        });

        Assert.AreEqual(new PluginPendingChange("Pending.B", "B", PluginPendingChangeKind.Added), changes.Single());
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetPendingChanges_NewFolderWithoutPluginAssembly_IsNotReported()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        WriteInstalled();

        var changes = PendingAfter(() =>
        {
            Directory.CreateDirectory(_fixture.Folder("Native"));
            File.WriteAllBytes(Path.Combine(_fixture.Folder("Native"), "native.dll"), [0x4D, 0x5A, 1, 2, 3]);
            TestPluginBuilder.Compile(_fixture.Folder("Library"), "Pending.Library", "public class Library { }");
        });

        Assert.IsEmpty(changes);
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetPendingChanges_LoadedFolderDeleted_ReportsRemoved()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        _fixture.AddPlugin("B", "Pending.B", "Pending.B");
        WriteInstalled();

        var changes = PendingAfter(() => Directory.Delete(_fixture.Folder("B"), true));

        Assert.AreEqual(new PluginPendingChange("Pending.B", "B", PluginPendingChangeKind.Removed),
            changes.Single());
    }

    [TestMethod]
    [DoNotParallelize]
    public void GetPendingChanges_MainDllReplaced_ReportsUpdated()
    {
        var main = _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        WriteInstalled("Pending.A");
        var loadedLength = new FileInfo(main).Length;

        var changes = PendingAfter(() =>
        {
            _fixture.AddPlugin("A", "Pending.A", "Pending.A", "public class AddedInTheNewVersion { }");
            Assert.AreNotEqual(loadedLength, new FileInfo(main).Length);
        });

        Assert.AreEqual(new PluginPendingChange("Pending.A", "A", PluginPendingChangeKind.Updated),
            changes.Single());
    }

    /// <summary>
    ///     Records the state as <see cref="PluginManager.Load" /> does, applies the change and compares
    /// </summary>
    private IReadOnlyList<PluginPendingChange> PendingAfter(Action change)
    {
        var loadSnapshot = PluginManager.LoadSnapshot;
        try
        {
            PluginManager.LoadSnapshot = PluginLoadSnapshot.Capture(new DirectoryInfo(_fixture.PluginsFolder),
                _fixture.Scan(), PluginExtensions.ParseInstalledPluginsFile(_installedFile), _installedFile, null);

            change();

            return PluginManager.GetPendingChanges();
        }
        finally
        {
            PluginManager.LoadSnapshot = loadSnapshot;
        }
    }

    private void WriteInstalled(params string[] systemNames)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_installedFile)!);
        File.WriteAllText(_installedFile, JsonSerializer.Serialize(systemNames));
    }
}
