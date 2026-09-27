using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Themes;

[TestClass]
public class ModernHeaderTests
{
    private static string Header => File.ReadAllText(Path.Combine(ModernThemeAssetsTests.ModernViews, "Shared", "Partials", "Header.cshtml"));

    [TestMethod]
    public void InlineMenu_IsVPre()
    {
        // category names are database content rendered inside the header's Vue island
        StringAssert.Matches(Header, new System.Text.RegularExpressions.Regex(@"class=""mdn-menu-row""[^>]*\bv-pre\b"));
    }

    [TestMethod]
    public void PromoBar_IsOutsideTheIsland()
    {
        var promo = Header.IndexOf("mdn-promo", StringComparison.Ordinal);
        var island = Header.IndexOf("<header", StringComparison.Ordinal);
        Assert.IsTrue(promo >= 0 && promo < island, "the promo bar must render before (outside) the <header> island");
    }

    [TestMethod]
    public void DrawersAndSearchModal_AreRenderedAfterTheHeader()
    {
        var headerEnd = Header.IndexOf("</header>", StringComparison.Ordinal);
        foreach (var id in new[] { "id=\"sidebar-menu\"", "id=\"sidebar-right\"", "id=\"search-box\"" })
            Assert.IsTrue(Header.IndexOf(id, StringComparison.Ordinal) > headerEnd, $"{id} must sit outside the sticky header");
    }

    [TestMethod]
    public void SearchTrigger_OpensTheSearchModal()
    {
        StringAssert.Contains(Header, "mdn-search-trigger");
        StringAssert.Contains(Header, "data-bs-target=\"#search-box\"");
    }
}
