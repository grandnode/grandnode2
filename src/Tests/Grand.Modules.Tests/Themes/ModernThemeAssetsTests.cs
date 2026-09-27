using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Themes;

/// <summary>
///     Modern is a thin layer over the Default views: only the views whose markup has to
///     change are copied, and the theme ships CSS and fonts but no JavaScript.
/// </summary>
[TestClass]
public class ModernThemeAssetsTests
{
    internal static string ThemeRoot => Path.Combine(Installer.RepositoryPaths.Root, "src", "Plugins", "Theme.Modern");
    internal static string ModernViews => Path.Combine(ThemeRoot, "Views", "Modern");
    private static string Content => Path.Combine(ThemeRoot, "Content");

    internal static readonly string[] ApprovedViews = {
        "_ViewImports.cshtml",
        "_ViewStart.cshtml",
        Path.Combine("Shared", "Partials", "Head.cshtml"),
        Path.Combine("Shared", "Partials", "Header.cshtml"),
        Path.Combine("Shared", "Partials", "CatalogProductView.cshtml"),
        Path.Combine("Catalog", "Partials", "Client", "CatalogProductGridView.cshtml"),
        Path.Combine("Shared", "Components", "Footer", "Default.cshtml")
    };

    internal static readonly string[] ApprovedContent = {
        "theme.jpg",
        Path.Combine("fonts", "geist-latin-var.woff2"),
        Path.Combine("fonts", "geist-latin-ext-var.woff2"),
        Path.Combine("fonts", "geist-mono-latin-var.woff2"),
        Path.Combine("fonts", "geist-mono-latin-ext-var.woff2"),
        Path.Combine("fonts", "OFL-Geist.txt"),
        Path.Combine("fonts", "README.md"),
        Path.Combine("css", "fonts.css"),
        Path.Combine("css", "tokens.css"),
        Path.Combine("css", "base.css"),
        Path.Combine("css", "header.css"),
        Path.Combine("css", "home.css"),
        Path.Combine("css", "catalog.css"),
        Path.Combine("css", "product.css"),
        Path.Combine("css", "content.css"),
        Path.Combine("css", "account-cart.css")
    };

    private static IEnumerable<string> Relative(string root) =>
        Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(root, f));

    [TestMethod]
    public void OnlyApprovedViewsAreOverridden()
    {
        var extra = Relative(ModernViews).Except(ApprovedViews).ToList();
        Assert.AreEqual(0, extra.Count, $"views outside the approved list: {string.Join(", ", extra)}");
    }

    [TestMethod]
    public void OnlyApprovedContentIsShipped()
    {
        var extra = Relative(Content).Except(ApprovedContent).ToList();
        Assert.AreEqual(0, extra.Count, $"content outside the approved list: {string.Join(", ", extra)}");
    }

    [TestMethod]
    public void ThemeShipsNoJavaScript()
    {
        Assert.AreEqual(0, Directory.GetFiles(Content, "*.js", SearchOption.AllDirectories).Length);
    }

    [TestMethod]
    public void Views_UseNoThemeResourceKeys()
    {
        var used = Directory.GetFiles(ModernViews, "*.cshtml", SearchOption.AllDirectories)
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"""(Theme\.Modern\.[^""]+)""").Select(m => m.Groups[1].Value))
            .Distinct().ToList();
        Assert.AreEqual(0, used.Count, $"Modern adds no resources, but views use: {string.Join(", ", used)}");
    }

    private static string Head => File.ReadAllText(Path.Combine(ModernViews, "Shared", "Partials", "Head.cshtml"));

    [TestMethod]
    public void Head_LinksEveryThemeStylesheetOnce_AfterTheDefaultStyles()
    {
        var head = Head;
        var defaultAt = head.IndexOf("/bundles/style.min.css", StringComparison.Ordinal);
        Assert.IsTrue(defaultAt >= 0, "the Default production stylesheet must be loaded");
        foreach (var css in ApprovedContent.Where(c => c.StartsWith("css")))
        {
            var url = "/Plugins/Theme.Modern/Content/" + css.Replace('\\', '/');
            var hits = Regex.Matches(head, Regex.Escape(url)).Count;
            Assert.AreEqual(1, hits, $"{url} linked {hits} times");
            Assert.IsTrue(head.IndexOf(url, StringComparison.Ordinal) > defaultAt, $"{url} must come after the Default styles");
        }
    }

    [TestMethod]
    public void Head_EveryThemeUrlExistsOnDisk()
    {
        foreach (Match m in Regex.Matches(Head, @"/Plugins/Theme\.Modern/Content/([^""]+)"""))
            Assert.IsTrue(File.Exists(Path.Combine(Content, m.Groups[1].Value.Replace('/', Path.DirectorySeparatorChar))), m.Value);
    }

    [TestMethod]
    public void Head_UsesTheDefaultRuntimeAndAppScripts()
    {
        StringAssert.Contains(Head, "/bundles/app.runtime.bundle.js");
        StringAssert.Contains(Head, "/theme/script/app.js");
        StringAssert.Contains(Head, "/assets/custom/script.js");
    }

    [TestMethod]
    public void FontsCss_EveryUrlExists()
    {
        var css = File.ReadAllText(Path.Combine(Content, "css", "fonts.css"));
        var urls = Regex.Matches(css, @"url\(""\.\./fonts/([^""]+)""\)").Select(m => m.Groups[1].Value).ToList();
        Assert.AreEqual(4, urls.Count);
        foreach (var u in urls)
            Assert.IsTrue(File.Exists(Path.Combine(Content, "fonts", u)), u);
    }

    [TestMethod]
    public void FontsCss_DeclaresLatinExtForBothFamilies()
    {
        var css = File.ReadAllText(Path.Combine(Content, "css", "fonts.css"));
        // U+0100-02BA covers ą ę ł ś ż and the rest of Latin Extended-A/B
        Assert.AreEqual(2, Regex.Matches(css, @"unicode-range:\s*U\+0100-02BA").Count);
        StringAssert.Contains(css, "font-family: \"Geist\"");
        StringAssert.Contains(css, "font-family: \"Geist Mono\"");
    }
}
