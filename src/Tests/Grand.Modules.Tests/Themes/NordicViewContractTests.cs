using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Themes;

[TestClass]
public class NordicViewContractTests
{
    private static string NordicViews => Path.Combine(Installer.RepositoryPaths.Root, "src", "Plugins", "Theme.Nordic", "Views", "Nordic");
    private static string DefaultViews => Path.Combine(Installer.RepositoryPaths.Root, "src", "Web", "Grand.Web", "Views");

    public static IEnumerable<object[]> CopiedViews() =>
        Directory.GetFiles(NordicViews, "*.cshtml", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(NordicViews, f))
            .Where(r => !r.StartsWith("_View"))
            .Select(r => new object[] { r });

    private static string Read(string root, string rel) => File.ReadAllText(Path.Combine(root, rel));

    private static SortedSet<string> Matches(string text, string pattern) =>
        new(Regex.Matches(text, pattern).Select(m => m.Groups[1].Value));

    [TestMethod]
    public void EveryCopiedViewHasADefaultCounterpart()
    {
        foreach (var row in CopiedViews())
            Assert.IsTrue(File.Exists(Path.Combine(DefaultViews, (string)row[0])), $"{row[0]} has no Default counterpart");
    }

    [TestMethod]
    [DynamicData(nameof(CopiedViews), DynamicDataSourceType.Method)]
    public void EveryCopiedViewKeepsDefaultModel(string rel) =>
        CollectionAssert.AreEqual(Matches(Read(DefaultViews, rel), @"@model\s+([^\r\n]+)").ToList(),
            Matches(Read(NordicViews, rel), @"@model\s+([^\r\n]+)").ToList(), rel);

    [TestMethod]
    [DynamicData(nameof(CopiedViews), DynamicDataSourceType.Method)]
    public void EveryCopiedViewKeepsWidgetZones(string rel)
    {
        const string zones = @"(?:widgetZone\s*=\s*""|widget-zone=""|WidgetZone\s*=\s*"")([^""]+)""";
        var missing = Matches(Read(DefaultViews, rel), zones).Except(Matches(Read(NordicViews, rel), zones)).ToList();
        Assert.AreEqual(0, missing.Count, $"{rel} dropped widget zones: {string.Join(", ", missing)}");
    }

    [TestMethod]
    [DynamicData(nameof(CopiedViews), DynamicDataSourceType.Method)]
    public void EveryCopiedViewKeepsDataHooksAndPartials(string rel)
    {
        var d = Read(DefaultViews, rel);
        var n = Read(NordicViews, rel);
        foreach (var pattern in new[] {
                     @"(data-[a-z-]+)=",                           // data attributes used by app.js / Vue
                     @"<partial name=""([^""]+)""",               // partials still rendered
                     @"InvokeAsync\(""([^""]+)""",                // view components still invoked
                     @"RouteUrl\(""([^""]+)"""                     // routes still linked
                 })
        {
            var missing = Matches(d, pattern).Except(Matches(n, pattern)).ToList();
            Assert.AreEqual(0, missing.Count, $"{rel} dropped {pattern}: {string.Join(", ", missing)}");
        }
    }
}
