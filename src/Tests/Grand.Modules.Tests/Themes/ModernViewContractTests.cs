using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Themes;

/// <summary>
///     Every view Modern overrides must keep the model, widget zones, data hooks, partials,
///     view components and routes of the Default view it replaces.
/// </summary>
[TestClass]
public class ModernViewContractTests
{
    private static string ModernViews => ModernThemeAssetsTests.ModernViews;
    private static string DefaultViews => Path.Combine(Installer.RepositoryPaths.Root, "src", "Web", "Grand.Web", "Views");

    public static IEnumerable<object[]> CopiedViews() =>
        Directory.GetFiles(ModernViews, "*.cshtml", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(ModernViews, f))
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
            Matches(Read(ModernViews, rel), @"@model\s+([^\r\n]+)").ToList(), rel);

    [TestMethod]
    [DynamicData(nameof(CopiedViews), DynamicDataSourceType.Method)]
    public void EveryCopiedViewKeepsWidgetZones(string rel)
    {
        const string zones = @"(?:widgetZone\s*=\s*""|widget-zone=""|WidgetZone\s*=\s*"")([^""]+)""";
        var missing = Matches(Read(DefaultViews, rel), zones).Except(Matches(Read(ModernViews, rel), zones)).ToList();
        Assert.AreEqual(0, missing.Count, $"{rel} dropped widget zones: {string.Join(", ", missing)}");
    }

    // Deliberate removals, each with its reason. Anything not listed here must be kept.
    private static readonly Dictionary<string, string[]> IntentionallyDropped = new() {
        // the search modal is gone (the box sits in the bar), and with it the searchModal view-model island
        [Path.Combine("Shared", "Partials", "Header.cshtml")] = new[] { "data-grand-vm" }
    };

    [TestMethod]
    [DynamicData(nameof(CopiedViews), DynamicDataSourceType.Method)]
    public void EveryCopiedViewKeepsDataHooksAndPartials(string rel)
    {
        var d = Read(DefaultViews, rel);
        var m = Read(ModernViews, rel);
        foreach (var pattern in new[] {
                     @"(data-[a-z-]+)=",
                     @"<partial name=""([^""]+)""",
                     @"InvokeAsync\(""([^""]+)""",
                     @"RouteUrl\(""([^""]+)"""
                 })
        {
            var allowed = IntentionallyDropped.TryGetValue(rel, out var list) ? list : Array.Empty<string>();
            var missing = Matches(d, pattern).Except(Matches(m, pattern)).Except(allowed).ToList();
            Assert.AreEqual(0, missing.Count, $"{rel} dropped {pattern}: {string.Join(", ", missing)}");
        }
    }
}
