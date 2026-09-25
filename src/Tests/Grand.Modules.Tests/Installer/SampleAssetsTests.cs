using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

[TestClass]
public class SampleAssetsTests
{
    private static readonly Regex SampleFile =
        new(@"""((?:product|category|brand|collection|blog|news|vendor)_[A-Za-z0-9_\-]+\.(?:jpg|jpeg|png))""", RegexOptions.Compiled);

    private static IEnumerable<string> ReferencedFiles() =>
        Directory.GetFiles(RepositoryPaths.InstallerServices, "*.cs")
            .SelectMany(f => SampleFile.Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value))
            .Distinct();

    [TestMethod]
    public void EverySampleFileReferencedByInstallerExists()
    {
        var missing = ReferencedFiles()
            .Where(name => !File.Exists(Path.Combine(RepositoryPaths.Samples, name)))
            .ToList();
        Assert.AreEqual(0, missing.Count, "Missing sample files: " + string.Join(", ", missing));
    }

    [TestMethod]
    [Ignore("enabled in Task 15")]
    public void NoOrphanSampleImages()
    {
        var referenced = ReferencedFiles().ToHashSet(StringComparer.OrdinalIgnoreCase);
        referenced.Add("default-theme.jpg");
        var orphans = Directory.GetFiles(RepositoryPaths.Samples)
            .Select(Path.GetFileName)
            .Where(n => n != null && !referenced.Contains(n) && !n.EndsWith(".md"))
            .ToList();
        Assert.AreEqual(0, orphans.Count, "Unreferenced sample files: " + string.Join(", ", orphans));
    }
}
