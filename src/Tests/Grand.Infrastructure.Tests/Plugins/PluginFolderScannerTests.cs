using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Tests.Plugins.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Plugins;

[TestClass]
public class PluginFolderScannerTests
{
    private static readonly string CoreAssemblyPath = typeof(PluginInfoAttribute).Assembly.Location;
    private static readonly HashSet<string> Host = ["Grand.Infrastructure"];

    private string _root;

    [TestInitialize]
    public void Init()
    {
        _root = Path.Combine(Path.GetTempPath(), "grand-plugin-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private string Folder(string name) => Path.Combine(_root, name);

    private string AddPlugin(string folder, string assemblyName, string systemName)
    {
        return TestPluginBuilder.Compile(Folder(folder), assemblyName, $$"""
            using Grand.Infrastructure.Plugins;
            [assembly: PluginInfo(SystemName = "{{systemName}}")]
            """, null, CoreAssemblyPath);
    }

    private PluginScanResult Scan(string skipPattern = null) =>
        PluginFolderScanner.Scan(new DirectoryInfo(_root), skipPattern, Host);

    [TestMethod]
    public void Scan_DuplicateSystemName_SecondFolderIsProblem()
    {
        AddPlugin("A", "Scan.A", "Scan.Same");
        AddPlugin("B", "Scan.B", "scan.same");

        var result = Scan();

        Assert.HasCount(1, result.Plugins);
        Assert.AreEqual("A", result.Plugins[0].Folder.Name);
        Assert.HasCount(1, result.Problems);
        Assert.AreEqual("B", result.Problems[0].Folder);
        Assert.AreEqual("scan.same", result.Problems[0].SystemNames.Single());
    }

    [TestMethod]
    public void Scan_SameDependencyTwoVersions_EachFolderKeepsItsOwn()
    {
        AddPlugin("A", "Scan.A", "Scan.A");
        AddPlugin("B", "Scan.B", "Scan.B");
        var low = TestPluginBuilder.Compile(Folder("A"), "Shared.Lib", "public class Lib { }", new Version(1, 0, 0, 0));
        var high = TestPluginBuilder.Compile(Folder("B"), "Shared.Lib", "public class Lib { }", new Version(2, 0, 0, 0));

        var result = Scan();

        //each plugin loads into its own context, so neither copy has to give way
        Assert.HasCount(2, result.Plugins);
        var a = result.Plugins.Single(x => x.Folder.Name == "A");
        var b = result.Plugins.Single(x => x.Folder.Name == "B");
        CollectionAssert.Contains(a.AssemblyPaths.ToList(), low);
        CollectionAssert.Contains(b.AssemblyPaths.ToList(), high);
        Assert.Contains("Shared.Lib", a.AssemblyNames);
        Assert.Contains("Shared.Lib", b.AssemblyNames);
        Assert.IsEmpty(result.Warnings);
    }

    [TestMethod]
    public void Scan_IncompatiblePluginShipsHigherVersion_CompatibleKeepsItsCopy()
    {
        AddPlugin("A", "Scan.A", "Scan.A");
        var own = TestPluginBuilder.Compile(Folder("A"), "Shared.Lib", "public class Lib { }", new Version(1, 0, 0, 0));
        TestPluginBuilder.Compile(Folder("B"), "Scan.B", """
            using Grand.Infrastructure.Plugins;
            [assembly: PluginInfo(SystemName = "Scan.B", SupportedVersion = "1.0")]
            """, null, CoreAssemblyPath);
        TestPluginBuilder.Compile(Folder("B"), "Shared.Lib", "public class Lib { }", new Version(2, 0, 0, 0));

        var result = Scan();

        CollectionAssert.Contains(result.Plugins.Single(x => x.Folder.Name == "A").AssemblyPaths.ToList(), own);
        Assert.IsEmpty(result.Warnings);
    }

    [TestMethod]
    public void Scan_HostAssemblyInPluginFolder_IsSkippedAndNotOwned()
    {
        AddPlugin("A", "Scan.A", "Scan.A");
        File.Copy(CoreAssemblyPath, Path.Combine(Folder("A"), "Grand.Infrastructure.dll"));

        var result = Scan();

        var plugin = result.Plugins.Single();
        Assert.IsFalse(plugin.AssemblyPaths.Any(x => Path.GetFileName(x) == "Grand.Infrastructure.dll"));
        Assert.DoesNotContain("Grand.Infrastructure", plugin.AssemblyNames);
        Assert.HasCount(1, result.Warnings);
    }

    [TestMethod]
    public void Scan_NativeDll_IsIgnoredWithoutProblem()
    {
        AddPlugin("A", "Scan.A", "Scan.A");
        File.WriteAllBytes(Path.Combine(Folder("A"), "native.dll"),
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(4096));

        var result = Scan();

        Assert.HasCount(1, result.Plugins);
        Assert.IsEmpty(result.Problems);
        Assert.IsFalse(result.Plugins[0].AssemblyPaths.Any(x => Path.GetFileName(x) == "native.dll"));
    }

    [TestMethod]
    public void Scan_SkipPattern_ExcludesMatchingFiles()
    {
        AddPlugin("A", "Scan.A", "Scan.A");
        TestPluginBuilder.Compile(Folder("A"), "Skipped.Lib", "public class Lib { }");

        var result = Scan("^Skipped");

        var plugin = result.Plugins.Single();
        Assert.IsFalse(plugin.AssemblyPaths.Any(x => Path.GetFileName(x) == "Skipped.Lib.dll"));
        Assert.DoesNotContain("Skipped.Lib", plugin.AssemblyNames);
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void Scan_UnreadableMainAssembly_IsProblemNamingTheFolder()
    {
        var path = AddPlugin("A", "Scan.A", "Scan.A");
        AddPlugin("B", "Scan.B", "Scan.B");

        PluginScanResult result;
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            result = Scan();
        }

        Assert.AreEqual("B", result.Plugins.Single().Folder.Name);
        var problem = result.Problems.Single();
        Assert.AreEqual("A", problem.Folder);
        Assert.Contains("Scan.A.dll", problem.Message);
    }

    [TestMethod]
    public void Scan_EmptyOrMissingSystemName_IsProblem()
    {
        AddPlugin("A", "Scan.A", "");
        TestPluginBuilder.Compile(Folder("B"), "Scan.B", """
            using Grand.Infrastructure.Plugins;
            [assembly: PluginInfo(FriendlyName = "No system name")]
            """, null, CoreAssemblyPath);

        var result = Scan();

        Assert.IsEmpty(result.Plugins);
        Assert.HasCount(2, result.Problems);
        Assert.IsTrue(result.Problems.All(x => x.Message == "has no SystemName"));
    }

    [TestMethod]
    public void Scan_BinAndTempUploadsFolders_AreIgnored()
    {
        AddPlugin("bin", "Scan.Bin", "Scan.Bin");
        AddPlugin("TempUploads", "Scan.Temp", "Scan.Temp");
        AddPlugin("A", "Scan.A", "Scan.A");

        var result = Scan();

        Assert.AreEqual("A", result.Plugins.Single().Folder.Name);
        Assert.IsEmpty(result.Problems);
    }
}
