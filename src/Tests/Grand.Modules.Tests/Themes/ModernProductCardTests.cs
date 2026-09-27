using Microsoft.VisualStudio.TestTools.UnitTesting;
using Theme.Modern;

namespace Grand.Modules.Tests.Themes;

[TestClass]
public class ModernProductCardTests
{
    [TestMethod]
    [DataRow(80d, 100d, 20)]
    [DataRow(249d, 399d, 38)]      // 37.59 -> 38
    [DataRow(66.67d, 100d, 33)]
    [DataRow(99.5d, 100d, 1)]      // 0.5 rounds away from zero, as Math.round does in the client card
    [DataRow(1d, 1000d, 100)]      // 99.9 -> 100
    public void DiscountPercent_OldPriceAboveCurrent_ReturnsRoundedPercent(double price, double oldPrice, int expected)
    {
        Assert.AreEqual(expected, ModernProductCard.DiscountPercent(price, oldPrice));
    }

    [TestMethod]
    [DataRow(100d, 0d)]            // no old price
    [DataRow(100d, 100d)]          // equal
    [DataRow(120d, 100d)]          // old price below current
    [DataRow(0d, 100d)]            // free / call for price
    [DataRow(-5d, 100d)]
    [DataRow(99.6d, 100d)]         // 0.4% rounds to 0 -> no badge
    public void DiscountPercent_NoMeaningfulDiscount_ReturnsNull(double price, double oldPrice)
    {
        Assert.IsNull(ModernProductCard.DiscountPercent(price, oldPrice));
    }

    [TestMethod]
    public void ServerCard_UsesTheHelper()
    {
        var view = File.ReadAllText(Path.Combine(ModernThemeAssetsTests.ModernViews, "Shared", "Partials", "CatalogProductView.cshtml"));
        StringAssert.Contains(view, "ModernProductCard.DiscountPercent(");
    }

    [TestMethod]
    public void ClientCard_RepeatsTheSameRule()
    {
        var view = File.ReadAllText(Path.Combine(ModernThemeAssetsTests.ModernViews, "Catalog", "Partials", "Client", "CatalogProductGridView.cshtml"));
        StringAssert.Contains(view, "product.ProductPrice.OldPriceValue > product.ProductPrice.PriceValue");
        StringAssert.Contains(view, "product.ProductPrice.PriceValue > 0");
        StringAssert.Contains(view, "Math.round((1 - product.ProductPrice.PriceValue / product.ProductPrice.OldPriceValue) * 100)");
        StringAssert.Contains(view, ">= 1");
    }
}
