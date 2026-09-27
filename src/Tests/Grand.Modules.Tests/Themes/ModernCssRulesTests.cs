using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Themes;

/// <summary>
///     Rules that keep the Modern layer usable on stores whose content differs from the sample
///     data. Each pins a defect found in review: a CSS regression here is otherwise only visible
///     in a browser.
/// </summary>
[TestClass]
public class ModernCssRulesTests
{
    private static string Css(string file) =>
        File.ReadAllText(Path.Combine(ModernThemeAssetsTests.ThemeRoot, "Content", "css", file));

    private static string Block(string css, string selectorPattern)
    {
        var m = Regex.Match(css, selectorPattern + @"[^{]*\{([^}]*)\}");
        return m.Success ? m.Groups[1].Value : "";
    }

    [TestMethod]
    public void ProseLinks_AreUnderlined()
    {
        // base.css strips link decoration globally; links inside running text must get it back
        var css = Css("content.css");
        var block = Block(css, @"\.page-body a");
        StringAssert.Contains(block, "text-decoration: underline");
    }

    [TestMethod]
    public void PromoBar_HidesThePageTitle()
    {
        // PageBlock renders an <h1 class="generalTitle"> when the page has a title: ink on ink
        StringAssert.Matches(Css("header.css"), new Regex(@"\.mdn-promo \.generalTitle\s*\{\s*display:\s*none"));
    }

    [TestMethod]
    public void MenuDropdowns_AreBoundedByTheViewport()
    {
        var block = Block(Css("header.css"), @"\n\.mdn-menu > li > ul\s*");
        StringAssert.Contains(block, "max-height");
        StringAssert.Contains(block, "overflow-y: auto");
    }

    [TestMethod]
    public void ProductPageMainImage_IsNotCropped()
    {
        var block = Block(Css("product.css"), @"\.product-details-page \.gallery img\.image\.main-image");
        StringAssert.Contains(block, "object-fit: contain");
    }

    [TestMethod]
    public void DarkFormControls_DoNotOverrideValidationBorders()
    {
        // a (0,2,0) dark rule on .form-control beats Bootstrap's .is-invalid border colour
        Assert.IsFalse(Regex.IsMatch(Css("base.css"), @"\[dark-theme=""true""\] \.form-control\b"));
    }

    [TestMethod]
    public void Important_IsOnlyUsedAgainstImportantUtilitiesOrReducedMotion()
    {
        // .input-group and .btn-group radius resets are not !important in Bootstrap or Default
        foreach (var file in Directory.GetFiles(Path.Combine(ModernThemeAssetsTests.ThemeRoot, "Content", "css"), "*.css"))
        {
            var css = File.ReadAllText(file);
            foreach (var bad in new[] { "input-group radius", "joined radii", "keep the block off" })
                Assert.IsFalse(css.Contains(bad), $"{Path.GetFileName(file)} still uses !important for: {bad}");
        }
    }
}
