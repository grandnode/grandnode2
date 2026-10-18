using Grand.Web.Common.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Infrastructure;

[TestClass]
public class PluginAssetContentTypesTests
{
    [TestMethod]
    [DataRow("/Payments.StripeCheckout/logo.jpg", "image/jpeg")]
    [DataRow("/Widgets.Slider/Assets/slider.CSS", "text/css")]
    [DataRow("/Theme.Modern/fonts/font.woff2", "font/woff2")]
    [DataRow("/Widgets.Slider/script.js", "text/javascript")]
    [DataRow("/Widgets.Slider/icon.svg", "image/svg+xml")]
    public void Provider_ServesStaticAssets(string subpath, string expected)
    {
        Assert.IsTrue(PluginAssetContentTypes.Provider.TryGetContentType(subpath, out var contentType));
        Assert.AreEqual(expected, contentType);
    }

    [TestMethod]
    [DataRow("/Payments.StripeCheckout/Payments.StripeCheckout.deps.json")]
    [DataRow("/Payments.StripeCheckout/Payments.StripeCheckout.dll")]
    [DataRow("/Payments.StripeCheckout/Payments.StripeCheckout.pdb")]
    [DataRow("/Payments.StripeCheckout/appsettings.xml")]
    [DataRow("/Payments.StripeCheckout/readme.txt")]
    [DataRow("/Payments.StripeCheckout/page.html")]
    public void Provider_DoesNotServePluginInternals(string subpath)
    {
        Assert.IsFalse(PluginAssetContentTypes.Provider.TryGetContentType(subpath, out _));
    }
}
