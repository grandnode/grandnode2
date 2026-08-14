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
    [DataRow("<iframe src=\"https://evil.tld/frame\"></iframe>", DisplayName = "iframe from unlisted host")]
    [DataRow("<iframe src=\"//evil.tld/frame\"></iframe>", DisplayName = "iframe protocol-relative unlisted host")]
    public void ContainsDisallowedRichText_FlagsExecutableMarkup(string payload)
    {
        Assert.IsTrue(_service.ContainsDisallowedRichText(payload), $"payload was not flagged: {payload}");
    }

    /// <summary>
    ///     Ordinary editor output must not be rejected, even the parts the allowlist reformats (e.g. an implied
    ///     &lt;tbody&gt; made explicit, or a css declaration re-spaced) - those are not removals.
    /// </summary>
    [TestMethod]
    [DataRow("<p>Hello <strong>world</strong></p>")]
    [DataRow("<ul><li>one</li><li>two</li></ul>")]
    [DataRow("<a href=\"https://grandnode.com\" target=\"_blank\">link</a>")]
    [DataRow("<a href=\"mailto:sales@grandnode.com\">mail</a>")]
    [DataRow("<img src=\"/content/images/p.jpg\" alt=\"product\" width=\"200\">")]
    [DataRow("<h2>Heading</h2><blockquote>quote</blockquote>")]
    [DataRow("<table class=\"table\"><tr><td>cell</td></tr></table>")]
    [DataRow("<p style=\"text-align:center\">centered</p>")]
    public void ContainsDisallowedRichText_AllowsLegitimateEditorOutput(string html)
    {
        Assert.IsFalse(_service.ContainsDisallowedRichText(html), $"legitimate markup was flagged: {html}");
    }

    [TestMethod]
    public void ContainsDisallowedRichText_AllowsIframeFromAllowedHost()
    {
        Assert.IsFalse(_service.ContainsDisallowedRichText(
            "<iframe src=\"https://www.youtube.com/embed/abc123\"></iframe>"));
    }

    [TestMethod]
    public void ContainsDisallowedRichText_AllowsRelativeIframe()
    {
        //the file manager inserts self-hosted video as a relative url; it cannot leave this origin
        Assert.IsFalse(_service.ContainsDisallowedRichText("<iframe src=\"/assets/media/promo.mp4\"></iframe>"));
    }

    [TestMethod]
    public void ContainsDisallowedRichText_FlagsEveryIframeWhenHostListIsConfiguredEmpty()
    {
        var service = new HtmlSanitizationService(new SecurityConfig { SanitizerAllowedIframeHosts = [] });

        Assert.IsTrue(service.ContainsDisallowedRichText(
            "<iframe src=\"https://www.youtube.com/embed/a\"></iframe>"));
    }

    [TestMethod]
    public void ContainsDisallowedRichText_HonoursConfiguredHosts()
    {
        var service = new HtmlSanitizationService(new SecurityConfig {
            SanitizerAllowedIframeHosts = ["*.trusted.tld"]
        });

        Assert.IsFalse(service.ContainsDisallowedRichText("<iframe src=\"https://cdn.trusted.tld/x\"></iframe>"));
        Assert.IsTrue(service.ContainsDisallowedRichText("<iframe src=\"https://www.youtube.com/embed/a\"></iframe>"));
    }

    [TestMethod]
    public void ContainsDisallowedRichText_AllowsInlineImageButNotInlineDocument()
    {
        Assert.IsFalse(_service.ContainsDisallowedRichText("<img src=\"data:image/png;base64,iVBORw0KGgo=\">"));
        Assert.IsTrue(_service.ContainsDisallowedRichText(
            "<a href=\"data:text/html,<script>alert(1)</script>\">x</a>"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ContainsDisallowedRichText_TreatsEmptyInputAsClean(string value)
    {
        Assert.IsFalse(_service.ContainsDisallowedRichText(value));
    }

    [TestMethod]
    public void ContainsMarkup_FlagsAnyTag()
    {
        Assert.IsTrue(_service.ContainsMarkup("<b>Bold</b> title"));
        Assert.IsTrue(_service.ContainsMarkup("<script>alert(1)</script>"));
        Assert.IsTrue(_service.ContainsMarkup("plain<br>text"));
    }

    [TestMethod]
    public void ContainsMarkup_AllowsPlainText()
    {
        Assert.IsFalse(_service.ContainsMarkup("Just a title"));
        Assert.IsFalse(_service.ContainsMarkup("Tea & Coffee"));
        Assert.IsFalse(_service.ContainsMarkup("Price < 10 and > 2"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void ContainsMarkup_TreatsEmptyInputAsClean(string value)
    {
        Assert.IsFalse(_service.ContainsMarkup(value));
    }

    /// <summary>
    ///     The per-call detection flag must not leak between unrelated calls on the same thread.
    /// </summary>
    [TestMethod]
    public void ContainsDisallowedRichText_DoesNotLeakStateBetweenCalls()
    {
        Assert.IsTrue(_service.ContainsDisallowedRichText("<script>alert(1)</script>"));
        Assert.IsFalse(_service.ContainsDisallowedRichText("<p>clean</p>"));
        Assert.IsTrue(_service.ContainsDisallowedRichText("<script>alert(1)</script>"));
    }

    /// <summary>
    ///     The singleton holds no mutable shared state - each call builds its own HtmlSanitizer around read-only
    ///     allowlists, with a result flag captured in a local closure - so concurrent calls on different threads
    ///     must never see each other's result, without needing a lock. Runs enough iterations across enough
    ///     threads that the old [ThreadStatic]-without-serialization design (and a naive shared mutable-field
    ///     design) would reliably produce a wrong verdict here.
    /// </summary>
    [TestMethod]
    public void ContainsDisallowedRichText_IsCorrectUnderConcurrentCalls()
    {
        const string dangerous = "<script>alert(1)</script>";
        const string clean = "<p>clean</p>";

        var wrongVerdicts = 0;
        Parallel.For(0, 2000, i =>
        {
            var service = _service; // shared singleton instance, as it is registered in DI
            if (i % 2 == 0)
            {
                if (!service.ContainsDisallowedRichText(dangerous)) Interlocked.Increment(ref wrongVerdicts);
            }
            else
            {
                if (service.ContainsDisallowedRichText(clean)) Interlocked.Increment(ref wrongVerdicts);
            }
        });

        Assert.AreEqual(0, wrongVerdicts, "a concurrent call observed another call's detection result");
    }

    /// <summary>
    ///     SecurityConfig.EnableHtmlSanitization is an operational kill switch: false must make both detection
    ///     methods report "nothing disallowed" even for payloads that are otherwise always flagged.
    /// </summary>
    [TestMethod]
    [DataRow("<script>alert(1)</script>")]
    [DataRow("<iframe src=\"https://evil.tld/frame\"></iframe>")]
    public void ContainsDisallowedRichText_AcceptsEverything_WhenSanitizationDisabled(string payload)
    {
        var service = new HtmlSanitizationService(new SecurityConfig { EnableHtmlSanitization = false });

        Assert.IsFalse(service.ContainsDisallowedRichText(payload));
    }

    [TestMethod]
    [DataRow("<script>alert(1)</script>")]
    [DataRow("<b>bold</b>")]
    public void ContainsMarkup_AcceptsEverything_WhenSanitizationDisabled(string payload)
    {
        var service = new HtmlSanitizationService(new SecurityConfig { EnableHtmlSanitization = false });

        Assert.IsFalse(service.ContainsMarkup(payload));
    }

    [TestMethod]
    public void EnableHtmlSanitization_DefaultsToTrue_WhenNotSetExplicitly()
    {
        // mirrors what configuration binding leaves behind when the appsettings.json key is absent
        var service = new HtmlSanitizationService(new SecurityConfig());

        Assert.IsTrue(service.ContainsDisallowedRichText("<script>alert(1)</script>"));
    }
}
