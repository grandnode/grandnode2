using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Tests.Plugins.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
public class PluginPendingChangesTests
{
    private PluginFolderFixture _fixture;
    private PluginStateStoreFixture _stateStore;

    [TestInitialize]
    public void Init()
    {
        _fixture = new PluginFolderFixture();
        _stateStore = new PluginStateStoreFixture();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _stateStore.Dispose();
        _fixture.Dispose();
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task GetPendingChanges_NothingChanged_IsEmpty()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        _fixture.AddPlugin("B", "Pending.B", "Pending.B");
        await WriteInstalled("Pending.A");

        var changes = await PendingAfter(() => { });

        Assert.IsEmpty(changes);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task GetPendingChanges_MarkedInstalledAfterLoad_ReportsInstalled()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        await WriteInstalled();

        var changes = await PendingAfter(() => WriteInstalled("Pending.A"));

        Assert.AreEqual(new PluginPendingChange("Pending.A", "A", PluginPendingChangeKind.Installed),
            changes.Single());
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task GetPendingChanges_MarkedUninstalledAfterLoad_ReportsUninstalled()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        await WriteInstalled("Pending.A");

        var changes = await PendingAfter(() => WriteInstalled());

        Assert.AreEqual(new PluginPendingChange("Pending.A", "A", PluginPendingChangeKind.Uninstalled),
            changes.Single());
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task GetPendingChanges_NewPluginFolder_ReportsAdded()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        await WriteInstalled();

        var changes = await PendingAfter(() =>
        {
            _fixture.AddPlugin("B", "Pending.B", "Pending.B");
            //a folder without a plugin assembly is not a plugin
            Directory.CreateDirectory(_fixture.Folder("NotAPlugin"));
        });

        Assert.AreEqual(new PluginPendingChange("Pending.B", "B", PluginPendingChangeKind.Added), changes.Single());
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task GetPendingChanges_NewFolderWithoutPluginAssembly_IsNotReported()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        await WriteInstalled();

        var changes = await PendingAfter(() =>
        {
            Directory.CreateDirectory(_fixture.Folder("Native"));
            File.WriteAllBytes(Path.Combine(_fixture.Folder("Native"), "native.dll"), [0x4D, 0x5A, 1, 2, 3]);
            TestPluginBuilder.Compile(_fixture.Folder("Library"), "Pending.Library", "public class Library { }");
        });

        Assert.IsEmpty(changes);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task GetPendingChanges_LoadedFolderDeleted_ReportsRemoved()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        _fixture.AddPlugin("B", "Pending.B", "Pending.B");
        await WriteInstalled();

        var changes = await PendingAfter(() => Directory.Delete(_fixture.Folder("B"), true));

        Assert.AreEqual(new PluginPendingChange("Pending.B", "B", PluginPendingChangeKind.Removed),
            changes.Single());
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task GetPendingChanges_MainDllReplaced_ReportsUpdated()
    {
        var main = _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        await WriteInstalled("Pending.A");
        var loadedLength = new FileInfo(main).Length;

        var changes = await PendingAfter(() =>
        {
            _fixture.AddPlugin("A", "Pending.A", "Pending.A", "public class AddedInTheNewVersion { }");
            Assert.AreNotEqual(loadedLength, new FileInfo(main).Length);
        });

        Assert.AreEqual(new PluginPendingChange("Pending.A", "A", PluginPendingChangeKind.Updated),
            changes.Single());
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task GetPendingChanges_OnlyDependencyReplaced_ReportsUpdated()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        TestPluginBuilder.Compile(_fixture.Folder("A"), "Pending.A.Dependency", "public class Dependency { }");
        await WriteInstalled("Pending.A");

        var changes = await PendingAfter(() => TestPluginBuilder.Compile(_fixture.Folder("A"), "Pending.A.Dependency",
            "public class Dependency { } public class AddedInTheNewVersion { }"));

        Assert.AreEqual(new PluginPendingChange("Pending.A", "A", PluginPendingChangeKind.Updated),
            changes.Single());
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task GetPendingChanges_PluginCopiedIntoAFolderThatHadNone_ReportsAdded()
    {
        _fixture.AddPlugin("A", "Pending.A", "Pending.A");
        Directory.CreateDirectory(_fixture.Folder("B"));
        await WriteInstalled();

        var changes = await PendingAfter(() => _fixture.AddPlugin("B", "Pending.B", "Pending.B"));

        Assert.AreEqual(new PluginPendingChange("Pending.B", "B", PluginPendingChangeKind.Added), changes.Single());
    }

    /// <summary>
    ///     Records the state as <see cref="PluginManager.Load" /> does, applies the change and compares
    /// </summary>
    private Task<IReadOnlyList<PluginPendingChange>> PendingAfter(Action change)
    {
        return PendingAfter(() =>
        {
            change();
            return Task.CompletedTask;
        });
    }

    private async Task<IReadOnlyList<PluginPendingChange>> PendingAfter(Func<Task> change)
    {
        var loadSnapshot = PluginManager.LoadSnapshot;
        try
        {
            PluginManager.LoadSnapshot = PluginLoadSnapshot.Capture(new DirectoryInfo(_fixture.PluginsFolder),
                _fixture.Scan(), _stateStore.Installed(), _stateStore.Store, null);

            await change();

            return await PluginManager.GetPendingChanges();
        }
        finally
        {
            PluginManager.LoadSnapshot = loadSnapshot;
        }
    }

    /// <summary>
    ///     Leaves exactly these plugins installed
    /// </summary>
    private async Task WriteInstalled(params string[] systemNames)
    {
        await _stateStore.Store.Clear();
        foreach (var systemName in systemNames)
            await _stateStore.Store.SetInstalled(systemName, true);
    }
}
