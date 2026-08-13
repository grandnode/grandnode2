using System.Net;
using AngleSharp.Dom;
using Ganss.Xss;
using Grand.Infrastructure.Configuration;

namespace Grand.Infrastructure.Security;

/// <summary>
///     Allowlist-based implementation over HtmlSanitizer.
///     Registered as a singleton: both underlying sanitizers are configured in the constructor and never mutated
///     afterwards, which is the condition under which HtmlSanitizer.Sanitize is safe to call concurrently.
/// </summary>
public class HtmlSanitizationService : IHtmlSanitizationService
{
    /// <summary>
    ///     Hosts allowed to be framed when the configuration does not name any. These are the embed hosts a store
    ///     realistically pastes into a page body; anything else has to be opted into explicitly.
    /// </summary>
    private static readonly string[] DefaultAllowedIframeHosts =
    [
        "youtube.com", "*.youtube.com",
        "youtube-nocookie.com", "*.youtube-nocookie.com",
        "vimeo.com", "*.vimeo.com",
        "google.com", "*.google.com"
    ];

    /// <summary>
    ///     Tags that are in the library defaults but have no place in store content: they either collect input or
    ///     submit it somewhere. A form rendered inside the admin panel is a credible phishing surface.
    /// </summary>
    private static readonly string[] InteractiveTags =
    [
        "form", "input", "button", "textarea", "select", "option", "optgroup", "label", "fieldset", "legend",
        "datalist", "output", "progress", "meter"
    ];

    private readonly string[] _allowedIframeHosts;
    private readonly HtmlSanitizer _plainTextSanitizer;
    private readonly HtmlSanitizer _richTextSanitizer;

    public HtmlSanitizationService(SecurityConfig securityConfig)
    {
        _allowedIframeHosts = ResolveAllowedIframeHosts(securityConfig);
        _richTextSanitizer = BuildRichTextSanitizer();
        _plainTextSanitizer = BuildPlainTextSanitizer();
    }

    public string SanitizeRichText(string html)
    {
        return string.IsNullOrWhiteSpace(html) ? html : _richTextSanitizer.Sanitize(html);
    }

    public string StripHtml(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        //the sanitizer re-encodes the text it keeps; decode so the caller gets plain text back and the view
        //layer encodes it exactly once
        return WebUtility.HtmlDecode(_plainTextSanitizer.Sanitize(text));
    }

    private static string[] ResolveAllowedIframeHosts(SecurityConfig securityConfig)
    {
        //absent configuration means "use the defaults"; a configured but empty list means "frame nothing"
        if (securityConfig?.SanitizerAllowedIframeHosts is null) return DefaultAllowedIframeHosts;

        return securityConfig.SanitizerAllowedIframeHosts
            .Where(host => !string.IsNullOrWhiteSpace(host))
            .Select(host => host.Trim())
            .ToArray();
    }

    private HtmlSanitizer BuildRichTextSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        //the library defaults already exclude script, object, embed, svg, style and every on* handler, because
        //only allowlisted tags and attributes survive; these are the deviations from those defaults
        foreach (var tag in InteractiveTags) sanitizer.AllowedTags.Remove(tag);

        //framed media, restricted to allowlisted hosts by OnFilterUrl below
        sanitizer.AllowedTags.Add("iframe");
        sanitizer.AllowedAttributes.Add("allowfullscreen");
        sanitizer.AllowedAttributes.Add("frameborder");
        sanitizer.AllowedAttributes.Add("allow");
        sanitizer.AllowedAttributes.Add("loading");

        //editors emit class names for tables, images and alignment; dropping them would visibly change content
        //that is already in the database
        sanitizer.AllowedAttributes.Add("class");

        //data-* is not allowlisted per attribute, so it cannot be vetted; it is also read by the storefront
        //scripts, which makes it a way to influence behaviour from stored content
        sanitizer.AllowDataAttributes = false;

        sanitizer.AllowedSchemes.Add("mailto");
        sanitizer.AllowedSchemes.Add("tel");
        //data: is accepted only for images, and only on img/src - enforced in OnFilterUrl
        sanitizer.AllowedSchemes.Add("data");

        sanitizer.FilterUrl += OnFilterUrl;
        sanitizer.PostProcessDom += OnPostProcessDom;

        return sanitizer;
    }

    private static HtmlSanitizer BuildPlainTextSanitizer()
    {
        return new HtmlSanitizer(new HtmlSanitizerOptions {
            AllowedTags = new HashSet<string>(),
            AllowedAttributes = new HashSet<string>(),
            AllowedSchemes = new HashSet<string>(),
            AllowedCssProperties = new HashSet<string>(),
            UriAttributes = new HashSet<string>()
        }) {
            //without this the text inside a removed tag would be discarded along with the tag
            KeepChildNodes = true
        };
    }

    private void OnFilterUrl(object sender, FilterUrlEventArgs e)
    {
        var url = e.SanitizedUrl ?? e.OriginalUrl;
        if (string.IsNullOrWhiteSpace(url)) return;

        var tagName = e.Tag?.NodeName;

        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            //data:text/html is a script execution vector; an inline image is not
            var isInlineImage = string.Equals(tagName, "IMG", StringComparison.OrdinalIgnoreCase) &&
                                url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);
            if (!isInlineImage) e.SanitizedUrl = null;
            return;
        }

        if (string.Equals(tagName, "IFRAME", StringComparison.OrdinalIgnoreCase) && !IsAllowedIframeUrl(url))
            e.SanitizedUrl = null;
    }

    /// <summary>
    ///     Drops iframes whose src was rejected by <see cref="OnFilterUrl" />. The attribute filter can only remove
    ///     the attribute, which would leave a src-less frame behind.
    /// </summary>
    private static void OnPostProcessDom(object sender, PostProcessDomEventArgs e)
    {
        var framesWithoutSource = e.Document.QuerySelectorAll("iframe")
            .Where(frame => string.IsNullOrEmpty(frame.GetAttribute("src")))
            .ToList();

        foreach (var frame in framesWithoutSource) frame.Remove();
    }

    private bool IsAllowedIframeUrl(string url)
    {
        if (_allowedIframeHosts.Length == 0) return false;

        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri)) return false;

        //a relative url cannot leave this origin - it is how the file manager inserts self-hosted video
        if (!uri.IsAbsoluteUri) return true;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;

        return _allowedIframeHosts.Any(allowed => IsHostMatch(uri.Host, allowed));
    }

    private static bool IsHostMatch(string host, string allowed)
    {
        if (allowed.StartsWith("*.", StringComparison.Ordinal))
            return host.EndsWith(allowed[1..], StringComparison.OrdinalIgnoreCase);

        return string.Equals(host, allowed, StringComparison.OrdinalIgnoreCase);
    }
}
