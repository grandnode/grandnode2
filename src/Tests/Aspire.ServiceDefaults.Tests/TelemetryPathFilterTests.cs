using Aspire.ServiceDefaults.Telemetry;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspire.ServiceDefaults.Tests;

[TestClass]
public class TelemetryPathFilterTests
{
    private static readonly PathString[] ExcludePaths = ["/health", "/_content", "/bundles"];
    private static readonly string[] ExcludeExtensions = [".css", ".jpg", ".woff2"];

    [TestMethod]
    [DataRow("/health", false)]
    [DataRow("/health/live", false)]
    [DataRow("/HEALTH/ready", false)]
    [DataRow("/_content/Grand.Web/site.css", false)]
    [DataRow("/bundles/app.js", false)]
    [DataRow("/healthy", true)]
    [DataRow("/", true)]
    [DataRow("/cart", true)]
    public void ShouldTrace_MatchesWholeSegments(string path, bool expected)
    {
        Assert.AreEqual(expected, TelemetryPathFilter.ShouldTrace(new PathString(path), ExcludePaths, []));
    }

    [TestMethod]
    [DataRow("/Plugins/Payments.StripeCheckout/logo.jpg", false)]
    [DataRow("/Plugins/Widgets.Slider/Assets/slider.CSS", false)]
    [DataRow("/Plugins/Theme/fonts/font.woff2", false)]
    [DataRow("/Plugins/PaymentStripeCheckout/WebHook", true)]
    [DataRow("/Plugins/Payments.StripeCheckout/Payments.StripeCheckout.dll", true)]
    [DataRow("/robots.txt", true)]
    [DataRow("/v1.css/product", true)]
    [DataRow("/", true)]
    public void ShouldTrace_MatchesExtensionOfLastSegment(string path, bool expected)
    {
        Assert.AreEqual(expected, TelemetryPathFilter.ShouldTrace(new PathString(path), [], ExcludeExtensions));
    }

    [TestMethod]
    public void ShouldTrace_NothingExcluded_TracesEverything()
    {
        Assert.IsTrue(TelemetryPathFilter.ShouldTrace(new PathString("/health/site.css"), [], []));
    }
}
