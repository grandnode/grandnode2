using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Theme.Nordic;

namespace Grand.Modules.Tests.Themes;

[TestClass]
public class NordicThemeResourcesTests
{
    private static string NordicViews => Path.Combine(Installer.RepositoryPaths.Root, "src", "Plugins", "Theme.Nordic", "Views", "Nordic");

    [TestMethod]
    public void EveryThemeResourceUsedInViewsIsInstalledByThePlugin()
    {
        var used = Directory.GetFiles(NordicViews, "*.cshtml", SearchOption.AllDirectories)
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"Loc\[""(Theme\.Nordic\.[^""]+)""").Select(m => m.Groups[1].Value))
            .Distinct()
            .ToList();

        var missing = used.Where(key => !NordicThemePlugin.Resources.ContainsKey(key)).ToList();
        Assert.AreEqual(0, missing.Count, $"not installed by NordicThemePlugin: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void ThemeResourcesAreNamespacedAndHaveValues()
    {
        foreach (var (name, value) in NordicThemePlugin.Resources)
        {
            StringAssert.StartsWith(name, "Theme.Nordic.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(value), name);
        }
    }
}
