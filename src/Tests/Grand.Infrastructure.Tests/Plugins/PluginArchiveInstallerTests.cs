using System.IO.Compression;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Tests.Plugins.Fixtures;
using Grand.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
public class PluginArchiveInstallerTests
{
    private static readonly string CoreAssemblyPath = typeof(PluginInfoAttribute).Assembly.Location;

    private string _root;
    private string _plugins;
    private string _build;

    [TestInitialize]
    public void Init()
    {
        _root = Path.Combine(Path.GetTempPath(), "grand-plugin-tests", Guid.NewGuid().ToString("N"));
        _plugins = Path.Combine(_root, "Plugins");
        _build = Path.Combine(_root, "build");
        Directory.CreateDirectory(_plugins);
        Directory.CreateDirectory(_build);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private PluginArchiveInstaller CreateInstaller() => new(_plugins, NullLogger.Instance);

    private static string Source(string systemName, string attributeExtra = "") => $$"""
        using Grand.Infrastructure.Plugins;
        [assembly: PluginInfo(SystemName = "{{systemName}}", FriendlyName = "Test"{{attributeExtra}})]
        """;

    /// <summary>Compiles a plugin into a fresh folder named <paramref name="folderName" /> and returns that folder.</summary>
    private string BuildPluginFolder(string folderName, string systemName, string attributeExtra = "")
    {
        var folder = Path.Combine(_build, Guid.NewGuid().ToString("N"), folderName);
        TestPluginBuilder.Compile(folder, systemName, Source(systemName, attributeExtra), null, CoreAssemblyPath);
        return folder;
    }

    private string ZipFolder(string folder, bool includeBaseDirectory = true)
    {
        var zip = Path.Combine(_build, Guid.NewGuid().ToString("N") + ".zip");
        ZipFile.CreateFromDirectory(folder, zip, CompressionLevel.Optimal, includeBaseDirectory);
        return zip;
    }

    private string ZipEntries(params (string Name, byte[] Content)[] entries)
    {
        var zip = Path.Combine(_build, Guid.NewGuid().ToString("N") + ".zip");
        using var archive = ZipFile.Open(zip, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            var entry = archive.CreateEntry(name);
            using var stream = entry.Open();
            stream.Write(content);
        }

        return zip;
    }

    private static string Hash(string folder) => string.Join("|", Directory
        .GetFiles(folder, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
        .Select(x => Path.GetRelativePath(folder, x) + ":" + Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(x)))));

    [TestMethod]
    public void Install_StandardPluginWithoutDeclaredVersion_Installs()
    {
        var folder = BuildPluginFolder("Tax.FixedRate", "Tax.FixedRate");
        var zip = ZipFolder(folder);

        var result = CreateInstaller().Install(zip);

        Assert.AreEqual("Tax.FixedRate", result.FolderName);
        Assert.AreEqual("Tax.FixedRate", result.Manifest.SystemName);
        Assert.IsTrue(File.Exists(Path.Combine(_plugins, "Tax.FixedRate", "Tax.FixedRate.dll")));
        Assert.IsFalse(Directory.Exists(Path.Combine(_plugins, "TempUploads")) &&
                       Directory.EnumerateFileSystemEntries(Path.Combine(_plugins, "TempUploads")).Any());
    }

    [TestMethod]
    public void Install_ArchiveWithoutDirectoryEntries_Installs()
    {
        var folder = BuildPluginFolder("Tax.FixedRate", "Tax.FixedRate");
        var zip = ZipEntries(("Tax.FixedRate/Tax.FixedRate.dll", File.ReadAllBytes(Path.Combine(folder, "Tax.FixedRate.dll"))));

        CreateInstaller().Install(zip);

        Assert.IsTrue(File.Exists(Path.Combine(_plugins, "Tax.FixedRate", "Tax.FixedRate.dll")));
    }

    [TestMethod]
    public void Install_BackslashEntryNames_Installs()
    {
        var folder = BuildPluginFolder("Tax.FixedRate", "Tax.FixedRate");
        var zip = ZipEntries(
            ("Tax.FixedRate\\Tax.FixedRate.dll", File.ReadAllBytes(Path.Combine(folder, "Tax.FixedRate.dll"))),
            ("Tax.FixedRate\\Views\\a.txt", [1, 2, 3]));

        CreateInstaller().Install(zip);

        Assert.IsTrue(File.Exists(Path.Combine(_plugins, "Tax.FixedRate", "Tax.FixedRate.dll")));
        Assert.IsTrue(File.Exists(Path.Combine(_plugins, "Tax.FixedRate", "Views", "a.txt")));
    }

    [TestMethod]
    public void Install_ArchiveWrappedInPluginsFolder_Installs()
    {
        var folder = BuildPluginFolder("Tax.FixedRate", "Tax.FixedRate");
        var zip = ZipEntries(("Plugins/Tax.FixedRate/Tax.FixedRate.dll", File.ReadAllBytes(Path.Combine(folder, "Tax.FixedRate.dll"))));

        var result = CreateInstaller().Install(zip);

        Assert.AreEqual("Tax.FixedRate", result.FolderName);
        Assert.IsTrue(File.Exists(Path.Combine(_plugins, "Tax.FixedRate", "Tax.FixedRate.dll")));
        Assert.IsFalse(Directory.Exists(Path.Combine(_plugins, "Plugins")));
    }

    [TestMethod]
    public void Install_TwoRootFolders_IsRejected()
    {
        var a = BuildPluginFolder("Tax.A", "Tax.A");
        var b = BuildPluginFolder("Tax.B", "Tax.B");
        var zip = ZipEntries(
            ("Tax.A/Tax.A.dll", File.ReadAllBytes(Path.Combine(a, "Tax.A.dll"))),
            ("Tax.B/Tax.B.dll", File.ReadAllBytes(Path.Combine(b, "Tax.B.dll"))));

        Assert.ThrowsExactly<GrandException>(() => CreateInstaller().Install(zip));

        Assert.IsFalse(Directory.Exists(Path.Combine(_plugins, "Tax.A")));
        Assert.IsFalse(Directory.Exists(Path.Combine(_plugins, "Tax.B")));
    }

    [TestMethod]
    public void Install_DllInSubfolderOfRoot_DoesNotTouchOtherPlugin()
    {
        var existing = BuildPluginFolder("Tax.FixedRate", "Tax.FixedRate");
        var target = Path.Combine(_plugins, "Tax.FixedRate");
        Directory.CreateDirectory(target);
        File.Copy(Path.Combine(existing, "Tax.FixedRate.dll"), Path.Combine(target, "Tax.FixedRate.dll"));
        var before = Hash(target);

        var inner = BuildPluginFolder("Inner", "X");
        var zip = ZipEntries(("Tax.FixedRate/Inner/X.dll", File.ReadAllBytes(Path.Combine(inner, "X.dll"))));

        Assert.ThrowsExactly<GrandException>(() => CreateInstaller().Install(zip));

        Assert.AreEqual(before, Hash(target));
    }

    [TestMethod]
    public void Install_OtherVersion_IsRejected()
    {
        var folder = BuildPluginFolder("Tax.Old", "Tax.Old", ", SupportedVersion = \"1.0\"");
        var zip = ZipFolder(folder);

        Assert.ThrowsExactly<GrandException>(() => CreateInstaller().Install(zip));

        Assert.IsFalse(Directory.Exists(Path.Combine(_plugins, "Tax.Old")));
    }

    [TestMethod]
    public void Install_SystemNameUsedByAnotherFolder_IsRejected()
    {
        var existing = BuildPluginFolder("Tax.FixedRate", "Tax.FixedRate");
        var target = Path.Combine(_plugins, "Tax.FixedRate");
        Directory.CreateDirectory(target);
        File.Copy(Path.Combine(existing, "Tax.FixedRate.dll"), Path.Combine(target, "Tax.FixedRate.dll"));

        var copy = BuildPluginFolder("Tax.FixedRateCopy", "tax.fixedrate");
        var zip = ZipFolder(copy);

        Assert.ThrowsExactly<GrandException>(() => CreateInstaller().Install(zip));

        Assert.IsFalse(Directory.Exists(Path.Combine(_plugins, "Tax.FixedRateCopy")));
    }

    [TestMethod]
    public void Install_TooManyEntries_IsRejected()
    {
        var entries = Enumerable.Range(0, PluginArchiveInstaller.MaxEntries + 1)
            .Select(x => ($"Tax.Big/f{x}.txt", Array.Empty<byte>())).ToArray();
        var zip = ZipEntries(entries);

        Assert.ThrowsExactly<GrandException>(() => CreateInstaller().Install(zip));

        Assert.IsFalse(Directory.Exists(Path.Combine(_plugins, "Tax.Big")));
    }

    [TestMethod]
    public void Install_ReplacesExistingVersion()
    {
        var target = Path.Combine(_plugins, "Tax.FixedRate");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "old.txt"), "old");

        var folder = BuildPluginFolder("Tax.FixedRate", "Tax.FixedRate");
        CreateInstaller().Install(ZipFolder(folder));

        Assert.IsFalse(File.Exists(Path.Combine(target, "old.txt")));
        Assert.IsTrue(File.Exists(Path.Combine(target, "Tax.FixedRate.dll")));
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void Install_TargetFileLocked_LeavesPreviousVersion()
    {
        var old = BuildPluginFolder("Tax.FixedRate", "Tax.FixedRate");
        var target = Path.Combine(_plugins, "Tax.FixedRate");
        Directory.CreateDirectory(target);
        var oldDll = Path.Combine(target, "Tax.FixedRate.dll");
        File.Copy(Path.Combine(old, "Tax.FixedRate.dll"), oldDll);
        File.WriteAllText(Path.Combine(target, "other.txt"), "keep");
        var before = Hash(target);

        var zip = ZipFolder(BuildPluginFolder("Tax.FixedRate", "Tax.FixedRate"));

        using (new FileStream(oldDll, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.ThrowsExactly<GrandException>(() => CreateInstaller().Install(zip));
        }

        Assert.AreEqual(before, Hash(target));
    }

    [TestMethod]
    [DataRow("bin")]
    [DataRow("TempUploads")]
    [DataRow("..")]
    public void Install_ReservedFolderName_IsRejected(string name)
    {
        var folder = BuildPluginFolder(name, "Tax.Reserved");
        var zip = ZipEntries(($"{name}/Tax.Reserved.dll", File.ReadAllBytes(Path.Combine(folder, "Tax.Reserved.dll"))));

        Assert.ThrowsExactly<GrandException>(() => CreateInstaller().Install(zip));
    }

    [TestMethod]
    public void Install_RealSizeExceedsLimitDespiteForgedHeader_IsRejected()
    {
        var content = System.Security.Cryptography.RandomNumberGenerator.GetBytes(20_000);
        var zip = ZipEntries(("Tax.Big/big.bin", content));
        //understate the uncompressed size in the central directory header
        var bytes = File.ReadAllBytes(zip);
        var signature = new byte[] { 0x50, 0x4B, 0x01, 0x02 };
        var at = bytes.AsSpan().LastIndexOf(signature);
        BitConverter.GetBytes(1).CopyTo(bytes, at + 24);
        File.WriteAllBytes(zip, bytes);
        var installer = new PluginArchiveInstaller(_plugins, NullLogger.Instance) { UncompressedBytesLimit = 1000 };

        Assert.ThrowsExactly<GrandException>(() => installer.Install(zip));

        Assert.IsFalse(Directory.Exists(Path.Combine(_plugins, "Tax.Big")));
    }

    [TestMethod]
    public void Install_ZipSlipEntry_IsRejected()
    {
        var folder = BuildPluginFolder("Tax.Slip", "Tax.Slip");
        var zip = ZipEntries(("../x.dll", File.ReadAllBytes(Path.Combine(folder, "Tax.Slip.dll"))));

        Assert.ThrowsExactly<GrandException>(() => CreateInstaller().Install(zip));

        Assert.IsFalse(File.Exists(Path.Combine(_root, "x.dll")));
    }

    [TestMethod]
    public void Install_RollbackFails_KeepsPreviousVersion()
    {
        var target = Path.Combine(_plugins, "Tax.FixedRate");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "old.txt"), "old");
        var zip = ZipFolder(BuildPluginFolder("Tax.FixedRate", "Tax.FixedRate"));
        var calls = 0;
        var installer = new PluginArchiveInstaller(_plugins, NullLogger.Instance) {
            MoveDirectory = (from, to) => {
                //the move aside succeeds; moving the new folder in and the rollback both fail
                if (calls++ == 0) Directory.Move(from, to);
                else throw new IOException("simulated");
            }
        };

        var ex = Assert.Throws<GrandException>(() => installer.Install(zip));

        StringAssert.Contains(ex.Message, "preserved");
        var kept = Directory.GetFiles(_plugins, "old.txt", SearchOption.AllDirectories);
        Assert.AreEqual(1, kept.Length);
        Assert.AreEqual("old", File.ReadAllText(kept[0]));
    }
}
