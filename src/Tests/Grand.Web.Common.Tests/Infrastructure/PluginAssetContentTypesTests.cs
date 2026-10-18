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
    [DataRow("/Shipping.ShippingPoint/Docs/Terms.PDF", "application/pdf")]
    public void Create_ServesStaticAssets(string subpath, string expected)
    {
        var provider = PluginAssetContentTypes.Create(null);

        Assert.IsTrue(provider.TryGetContentType(subpath, out var contentType));
        Assert.AreEqual(expected, contentType);
    }

    [TestMethod]
    [DataRow("/Payments.StripeCheckout/Payments.StripeCheckout.deps.json")]
    [DataRow("/Payments.StripeCheckout/Payments.StripeCheckout.dll")]
    [DataRow("/Payments.StripeCheckout/Payments.StripeCheckout.pdb")]
    [DataRow("/Payments.StripeCheckout/appsettings.xml")]
    [DataRow("/Payments.StripeCheckout/readme.txt")]
    [DataRow("/Payments.StripeCheckout/page.html")]
    [DataRow("/Payments.StripeCheckout/video.mp4")]
    public void Create_DoesNotServePluginInternals(string subpath)
    {
        Assert.IsFalse(PluginAssetContentTypes.Create(null).TryGetContentType(subpath, out _));
    }

    [TestMethod]
    [DataRow("/Widgets.Video/intro.mp4", "video/mp4")]
    [DataRow("/Widgets.Video/archive.zip", "application/x-zip-compressed")]
    [DataRow("/Widgets.Video/data.custom", "application/octet-stream")]
    public void Create_AdditionalExtensions_AreServed(string subpath, string expected)
    {
        var provider = PluginAssetContentTypes.Create([".mp4", "ZIP", " custom ", ""]);

        Assert.IsTrue(provider.TryGetContentType(subpath, out var contentType));
        Assert.AreEqual(expected, contentType);
    }

    [TestMethod]
    [DataRow("/Payments.StripeCheckout/Payments.StripeCheckout.dll")]
    [DataRow("/Payments.StripeCheckout/Payments.StripeCheckout.pdb")]
    [DataRow("/Payments.StripeCheckout/tool.exe")]
    [DataRow("/Payments.StripeCheckout/web.config")]
    public void Create_AssembliesAndBuildFiles_AreNeverServed(string subpath)
    {
        var provider = PluginAssetContentTypes.Create([".dll", "pdb", ".EXE", ".config"]);

        Assert.IsFalse(provider.TryGetContentType(subpath, out _));
    }

    [TestMethod]
    [DataRow("/Payments.StripeCheckout/Payments.StripeCheckout.deps.json")]
    [DataRow("/Payments.StripeCheckout/Payments.StripeCheckout.DEPS.JSON")]
    [DataRow("/Payments.StripeCheckout/Payments.StripeCheckout.runtimeconfig.json")]
    [DataRow("/Payments.StripeCheckout/Payments.StripeCheckout.runtimeconfig.dev.json")]
    public void Create_JsonAllowed_BuildFilesAreStillNeverServed(string subpath)
    {
        var provider = PluginAssetContentTypes.Create([".json"]);

        Assert.IsFalse(provider.TryGetContentType(subpath, out _));
        Assert.IsTrue(provider.TryGetContentType("/Widgets.Slider/locale/en.json", out _));
    }
}
