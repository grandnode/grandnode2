using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Widgets.GoogleAnalytics.Components;

namespace Grand.Modules.Tests.Plugins.Widgets;

[TestClass]
public class GoogleAnalyticsScriptValueTests
{
    [TestMethod]
    [DataRow("O'Brien")]
    [DataRow("a\\'b")]
    [DataRow("</script><b>")]
    [DataRow("line\nbreak")]
    [DataRow("Kraków")]
    public void FixIllegalJavaScriptChars_StaysOneStringWithTheSameValue(string value)
    {
        var encoded = WidgetsGoogleAnalyticsViewComponent.FixIllegalJavaScriptChars(value);

        //nothing that could end the quoted string or the script block is left as it was
        Assert.AreEqual(-1, encoded.IndexOfAny(['\'', '"', '<', '>', '\n']), encoded);
        //and read back as one string literal it is the original value
        Assert.AreEqual(value, JsonSerializer.Deserialize<string>("\"" + encoded + "\""));
    }

    [TestMethod]
    public void FixIllegalJavaScriptChars_KeepsPlainText()
    {
        Assert.AreEqual("Warsaw 01", WidgetsGoogleAnalyticsViewComponent.FixIllegalJavaScriptChars("Warsaw 01"));
    }
}
