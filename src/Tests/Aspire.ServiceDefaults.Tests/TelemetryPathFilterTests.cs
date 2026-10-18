using Aspire.ServiceDefaults.Telemetry;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspire.ServiceDefaults.Tests;

[TestClass]
public class TelemetryPathFilterTests
{
    private static readonly string[] ExcludePaths = ["/health", "/_content", "/bundles"];

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
        Assert.AreEqual(expected, TelemetryPathFilter.ShouldTrace(new PathString(path), ExcludePaths));
    }

    [TestMethod]
    public void ShouldTrace_NoExcludePaths_TracesEverything()
    {
        Assert.IsTrue(TelemetryPathFilter.ShouldTrace(new PathString("/health"), []));
    }
}
