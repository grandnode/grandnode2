using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Security;

[TestClass]
public class HtmlSanitizationServiceTests
{
    private HtmlSanitizationService _service;

    [TestInitialize]
    public void Init()
    {
        _service = new HtmlSanitizationService(new SecurityConfig());
    }

    /// <summary>
    ///     Every payload here defeated the regex blacklist that this service replaces.
    /// </summary>
    [TestMethod]
    [DataRow("<script>alert(1)</script>", DisplayName = "script element")]
    [DataRow("<script\nsrc=\"//evil.tld/x.js\"></script>", DisplayName = "script src across a newline")]
    [DataRow("<script>\nalert(1)\n</script>", DisplayName = "script body across newlines")]
    [DataRow("<img src=x onerror=alert(1)>", DisplayName = "onerror")]
    [DataRow("<img src=x onerror\n=alert(1)>", DisplayName = "onerror with newline before =")]
    [DataRow("<img src=x OnErRoR = alert(1)>", DisplayName = "onerror mixed case and spaced")]
    [DataRow("<svg onload=alert(1)>", DisplayName = "svg onload")]
    [DataRow("<input onfocus=alert(1) autofocus>", DisplayName = "onfocus autofocus")]
    [DataRow("<div onpointerover=alert(1)>x</div>", DisplayName = "onpointerover")]
    [DataRow("<div onanimationstart=alert(1)>x</div>", DisplayName = "onanimationstart")]
    [DataRow("<details ontoggle=alert(1) open>x</details>", DisplayName = "ontoggle")]
    [DataRow("<body onload=alert(1)>", DisplayName = "body onload")]
    [DataRow("<a href=\"javascript:alert(1)\">x</a>", DisplayName = "javascript scheme")]
    [DataRow("<a href=\"&#106;avascript:alert(1)\">x</a>", DisplayName = "entity-encoded javascript scheme")]
    [DataRow("<a href=\"JaVaScRiPt:alert(1)\">x</a>", DisplayName = "javascript scheme mixed case")]
    [DataRow("<iframe srcdoc=\"&lt;script&gt;alert(1)&lt;/script&gt;\"></iframe>", DisplayName = "iframe srcdoc")]
    [DataRow("<object data=\"evil.swf\"></object>", DisplayName = "object")]
    [DataRow("<embed src=\"evil.swf\">", DisplayName = "embed")]
    [DataRow("<form action=\"//evil.tld\"><input name=\"pw\"></form>", DisplayName = "phishing form")]
    [DataRow("<button formaction=\"javascript:alert(1)\">x</button>", DisplayName = "formaction")]
    [DataRow("<a href=\"data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==\">x</a>",
        DisplayName = "data:text/html")]
    [DataRow("<div style=\"background:url(javascript:alert(1))\">x</div>", DisplayName = "css url javascript")]
    [DataRow("<base href=\"//evil.tld/\">", DisplayName = "base tag")]
    [DataRow("<div v-html=\"x\" @click=\"y\" :id=\"z\">x</div>", DisplayName = "vue directives")]
    public void SanitizeRichText_RemovesExecutableMarkup(string payload)
    {
        var result = _service.SanitizeRichText(payload) ?? string.Empty;

        Assert.IsFalse(result.Contains("script", StringComparison.OrdinalIgnoreCase),
            $"script survived: {result}");
        Assert.IsFalse(result.Contains("javascript", StringComparison.OrdinalIgnoreCase),
            $"javascript scheme survived: {result}");
        Assert.IsFalse(result.Contains("alert(1)", StringComparison.OrdinalIgnoreCase),
            $"handler survived: {result}");
        Assert.IsFalse(result.Contains("srcdoc", StringComparison.OrdinalIgnoreCase),
            $"srcdoc survived: {result}");
        Assert.IsFalse(result.Contains("formaction", StringComparison.OrdinalIgnoreCase),
            $"formaction survived: {result}");
        Assert.IsFalse(result.Contains("<form", StringComparison.OrdinalIgnoreCase),
            $"form survived: {result}");
        Assert.IsFalse(result.Contains("<base", StringComparison.OrdinalIgnoreCase),
            $"base survived: {result}");
        Assert.IsFalse(result.Contains("v-html", StringComparison.OrdinalIgnoreCase),
            $"vue directive survived: {result}");
    }

    /// <summary>
    ///     Sanitizing on render touches content that is already in every existing database, so ordinary editor
    ///     output has to come through unchanged.
    /// </summary>
    [TestMethod]
    [DataRow("<p>Hello <strong>world</strong></p>")]
    [DataRow("<ul><li>one</li><li>two</li></ul>")]
    [DataRow("<a href=\"https://grandnode.com\" target=\"_blank\">link</a>")]
    [DataRow("<a href=\"mailto:sales@grandnode.com\">mail</a>")]
    [DataRow("<img src=\"/content/images/p.jpg\" alt=\"product\" width=\"200\">")]
    [DataRow("<h2>Heading</h2><blockquote>quote</blockquote>")]
    public void SanitizeRichText_KeepsLegitimateEditorOutput(string html)
    {
        var result = _service.SanitizeRichText(html);

        Assert.AreEqual(html, result);
    }

    /// <summary>
    ///     The sanitizer parses and re-serializes, so markup comes back normalized: an implied tbody is made
    ///     explicit and css declarations are re-formatted. Nothing is lost, but the stored string and the rendered
    ///     string are no longer byte-identical.
    /// </summary>
    [TestMethod]
    [DataRow("<table class=\"table\"><tr><td>cell</td></tr></table>",
        "<table class=\"table\"><tbody><tr><td>cell</td></tr></tbody></table>")]
    [DataRow("<p style=\"text-align:center\">centered</p>", "<p style=\"text-align: center\">centered</p>")]
    public void SanitizeRichText_NormalizesMarkupWithoutLosingIt(string html, string expected)
    {
        Assert.AreEqual(expected, _service.SanitizeRichText(html));
    }

    [TestMethod]
    public void SanitizeRichText_KeepsIframeFromAllowedHost()
    {
        const string html = "<iframe src=\"https://www.youtube.com/embed/abc123\"></iframe>";

        Assert.IsTrue(_service.SanitizeRichText(html).Contains("youtube.com/embed/abc123"));
    }

    [TestMethod]
    public void SanitizeRichText_KeepsRelativeIframe()
    {
        //the file manager inserts self-hosted video as a relative url; it cannot leave this origin
        const string html = "<iframe src=\"/assets/media/promo.mp4\"></iframe>";

        Assert.IsTrue(_service.SanitizeRichText(html).Contains("promo.mp4"));
    }

    [TestMethod]
    public void SanitizeRichText_RemovesIframeFromUnknownHost()
    {
        const string html = "<iframe src=\"https://evil.tld/frame\"></iframe>";

        Assert.IsFalse(_service.SanitizeRichText(html).Contains("evil.tld"));
        Assert.IsFalse(_service.SanitizeRichText(html).Contains("<iframe"));
    }

    [TestMethod]
    public void SanitizeRichText_RemovesEveryIframeWhenHostListIsConfiguredEmpty()
    {
        var service = new HtmlSanitizationService(new SecurityConfig { SanitizerAllowedIframeHosts = [] });

        Assert.IsFalse(service.SanitizeRichText("<iframe src=\"https://www.youtube.com/embed/a\"></iframe>")
            .Contains("<iframe"));
    }

    [TestMethod]
    public void SanitizeRichText_HonoursConfiguredHosts()
    {
        var service = new HtmlSanitizationService(new SecurityConfig {
            SanitizerAllowedIframeHosts = ["*.trusted.tld"]
        });

        Assert.IsTrue(service.SanitizeRichText("<iframe src=\"https://cdn.trusted.tld/x\"></iframe>")
            .Contains("cdn.trusted.tld"));
        Assert.IsFalse(service.SanitizeRichText("<iframe src=\"https://www.youtube.com/embed/a\"></iframe>")
            .Contains("youtube"));
    }

    [TestMethod]
    public void SanitizeRichText_KeepsInlineImageButNotInlineDocument()
    {
        const string image = "<img src=\"data:image/png;base64,iVBORw0KGgo=\">";

        Assert.IsTrue(_service.SanitizeRichText(image).Contains("data:image/png"));
        Assert.IsFalse(_service.SanitizeRichText("<a href=\"data:text/html,<script>alert(1)</script>\">x</a>")
            .Contains("data:text/html"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void SanitizeRichText_PassesThroughEmptyInput(string value)
    {
        Assert.AreEqual(value, _service.SanitizeRichText(value));
    }

    [TestMethod]
    public void StripHtml_RemovesMarkupAndKeepsText()
    {
        Assert.AreEqual("Bold title", _service.StripHtml("<b>Bold</b> title"));
        Assert.AreEqual("alert(1)", _service.StripHtml("<script>alert(1)</script>"));
        Assert.AreEqual("clickme", _service.StripHtml("<img src=x onerror=alert(1)>click<span>me</span>"));
    }

    [TestMethod]
    public void StripHtml_ReturnsDecodedText()
    {
        //the view layer encodes once; returning encoded text here would double-encode it
        Assert.AreEqual("Tea & Coffee", _service.StripHtml("Tea & Coffee"));
        Assert.AreEqual("Tea & Coffee", _service.StripHtml("<p>Tea &amp; Coffee</p>"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void StripHtml_PassesThroughEmptyInput(string value)
    {
        Assert.AreEqual(value, _service.StripHtml(value));
    }
}
