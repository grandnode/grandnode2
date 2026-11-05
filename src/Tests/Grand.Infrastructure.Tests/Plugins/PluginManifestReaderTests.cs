using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Tests.Plugins.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
public class PluginManifestReaderTests
{
    private static readonly string CoreAssemblyPath = typeof(PluginInfoAttribute).Assembly.Location;

    private string _folder;

    [TestInitialize]
    public void Init()
    {
        _folder = Path.Combine(Path.GetTempPath(), "grand-plugin-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    private static string PluginSource(string attributeArguments) => $$"""
        using Grand.Infrastructure.Plugins;
        [assembly: PluginInfo({{attributeArguments}})]
        """;

    [TestMethod]
    public void Read_PluginWithoutDeclaredVersion_ResolvesFromCoreReference()
    {
        var path = TestPluginBuilder.Compile(_folder, "Reader.NoVersion",
            PluginSource("""SystemName = "Reader.NoVersion", FriendlyName = "No version", Group = "Misc", Author = "me", Version = "1.2" """),
            null, CoreAssemblyPath);

        var manifest = PluginManifestReader.Read(path);

        Assert.IsNotNull(manifest);
        Assert.AreEqual("Reader.NoVersion", manifest.SystemName);
        Assert.AreEqual("No version", manifest.FriendlyName);
        Assert.AreEqual("Misc", manifest.Group);
        Assert.AreEqual("me", manifest.Author);
        Assert.AreEqual("1.2", manifest.Version);
        Assert.AreEqual(GrandVersion.SupportedPluginVersion, manifest.SupportedVersion);
    }

    [TestMethod]
    public void Read_DeclaredVersion_IsTrimmed()
    {
        var path = TestPluginBuilder.Compile(_folder, "Reader.Declared",
            PluginSource("""SystemName = "Reader.Declared", SupportedVersion = " 2.3 " """),
            null, CoreAssemblyPath);

        var manifest = PluginManifestReader.Read(path);

        Assert.AreEqual("2.3", manifest.SupportedVersion);
    }

    [TestMethod]
    public void Read_AssemblyWithoutAttribute_ReturnsNull()
    {
        var path = TestPluginBuilder.Compile(_folder, "Reader.Plain", "public class Plain { }");

        Assert.IsNull(PluginManifestReader.Read(path));
    }

    [TestMethod]
    public void Read_NonManagedFile_ReturnsNull()
    {
        var path = Path.Combine(_folder, "native.dll");
        File.WriteAllBytes(path, System.Security.Cryptography.RandomNumberGenerator.GetBytes(4096));

        Assert.IsNull(PluginManifestReader.Read(path));
        Assert.IsFalse(PluginManifestReader.IsManagedAssembly(path));
    }

    [TestMethod]
    public void Read_DoesNotLoadAssembly()
    {
        var path = TestPluginBuilder.Compile(_folder, "Reader.NotLoaded",
            PluginSource("""SystemName = "Reader.NotLoaded" """), null, CoreAssemblyPath);

        PluginManifestReader.Read(path);

        Assert.IsFalse(AppDomain.CurrentDomain.GetAssemblies()
            .Any(x => x.GetName().Name == "Reader.NotLoaded"));
    }
}
