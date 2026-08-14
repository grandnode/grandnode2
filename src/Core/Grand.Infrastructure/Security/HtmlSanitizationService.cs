using Ganss.Xss;
using Grand.Infrastructure.Configuration;

namespace Grand.Infrastructure.Security;

/// <summary>
///     Allowlist-based implementation over HtmlSanitizer. Sanitizes nothing itself - it runs the library's
///     allowlist and reports whether anything would have been removed, which is all a rejecting
///     <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute" /> needs.
///     Registered as a singleton, but holds no mutable shared state: the allowlists below (<see cref="ISet{T}" />
///     instances returned by a throwaway HtmlSanitizer during construction) are built once and never mutated
///     afterwards, so concurrent calls can read them freely. Each detection call builds its own short-lived
///     HtmlSanitizer around those shared, read-only allowlists, with event handlers that close over a local
///     "was anything removed" flag - so two concurrent calls can never observe or overwrite each other's result,
///     and no lock is needed.
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
    private readonly HtmlSanitizerOptions _richTextOptions;

    public HtmlSanitizationService(SecurityConfig securityConfig)
    {
        _allowedIframeHosts = ResolveAllowedIframeHosts(securityConfig);
        _richTextOptions = BuildRichTextOptions();
    }

    /// <summary>
    ///     Built once from a throwaway sanitizer's defaults, then reused - unmutated - as the constructor
    ///     argument for every sanitizer built per detection call below. HtmlSanitizerOptions only fills in what
    ///     it is given (verified: leaving UriAttributes/AllowedCssProperties/AllowedAtRules unset produces empty
    ///     sets, not the library defaults), so every allowlist a plain `new HtmlSanitizer()` would carry has to
    ///     be copied across explicitly, not just the ones this class customizes.
    /// </summary>
    private static HtmlSanitizerOptions BuildRichTextOptions()
    {
        var defaults = new HtmlSanitizer();

        //the library defaults already exclude script, object, embed, svg, style and every on* handler, because
        //only allowlisted tags and attributes survive; these are the deviations from those defaults
        foreach (var tag in InteractiveTags) defaults.AllowedTags.Remove(tag);

        //framed media, restricted to allowlisted hosts by OnFilterUrl below
        defaults.AllowedTags.Add("iframe");
        defaults.AllowedAttributes.Add("allowfullscreen");
        defaults.AllowedAttributes.Add("frameborder");
        defaults.AllowedAttributes.Add("allow");
        defaults.AllowedAttributes.Add("loading");

        //editors emit class names for tables, images and alignment
        defaults.AllowedAttributes.Add("class");

        defaults.AllowedSchemes.Add("mailto");
        defaults.AllowedSchemes.Add("tel");
        //data: is accepted only for images, and only on img/src - enforced in OnFilterUrl
        defaults.AllowedSchemes.Add("data");

        return new HtmlSanitizerOptions {
            AllowedTags = defaults.AllowedTags,
            AllowedAttributes = defaults.AllowedAttributes,
            AllowedSchemes = defaults.AllowedSchemes,
            AllowedCssClasses = defaults.AllowedClasses,
            AllowedCssProperties = defaults.AllowedCssProperties,
            AllowedAtRules = defaults.AllowedAtRules,
            UriAttributes = defaults.UriAttributes,
            UriListAttributes = defaults.UriListAttributes,
            AllowCssCustomProperties = defaults.AllowCssCustomProperties,
            //data-* is not allowlisted per attribute, so it cannot be vetted; it is also read by the storefront
            //scripts, which makes it a way to influence behaviour from stored content
            AllowDataAttributes = false
        };
    }

    public bool ContainsDisallowedRichText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return false;

        var disallowedContentSeen = false;

        void MarkDisallowed(object sender, EventArgs e)
        {
            disallowedContentSeen = true;
        }

        void OnFilterUrl(object sender, FilterUrlEventArgs e)
        {
            var url = e.SanitizedUrl ?? e.OriginalUrl;
            if (string.IsNullOrWhiteSpace(url)) return;

            var tagName = e.Tag?.NodeName;

            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                //data:text/html is a script execution vector; an inline image is not
                var isInlineImage = string.Equals(tagName, "IMG", StringComparison.OrdinalIgnoreCase) &&
                                    url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);
                if (!isInlineImage)
                {
                    e.SanitizedUrl = null;
                    disallowedContentSeen = true;
                }

                return;
            }

            if (string.Equals(tagName, "IFRAME", StringComparison.OrdinalIgnoreCase) &&
                !IsAllowedIframeUrl(url, _allowedIframeHosts))
            {
                e.SanitizedUrl = null;
                disallowedContentSeen = true;
            }
        }

        var sanitizer = new HtmlSanitizer(_richTextOptions);
        sanitizer.RemovingTag += MarkDisallowed;
        sanitizer.RemovingAttribute += MarkDisallowed;
        sanitizer.RemovingStyle += MarkDisallowed;
        sanitizer.RemovingAtRule += MarkDisallowed;
        sanitizer.RemovingCssClass += MarkDisallowed;
        sanitizer.RemovingComment += MarkDisallowed;
        sanitizer.FilterUrl += OnFilterUrl;

        var document = sanitizer.SanitizeDom(html);

        //a literal <html>/<head>/<body> tag in the input is merged into the parser's own document root, which
        //sits outside the per-element sanitization loop - RemovingAttribute never fires for its own attributes
        //even though everything inside it is correctly sanitized (verified: <body onload=alert(1)> survives
        //with the handler intact). Rich-text content is always a fragment and never legitimately needs
        //attributes on that wrapper, so any attribute there means the raw input smuggled one in.
        if (HasOwnAttributes(document.DocumentElement) || HasOwnAttributes(document.Head) ||
            HasOwnAttributes(document.Body))
            disallowedContentSeen = true;

        return disallowedContentSeen;
    }

    public bool ContainsMarkup(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var disallowedContentSeen = false;

        void MarkDisallowed(object sender, EventArgs e)
        {
            disallowedContentSeen = true;
        }

        var sanitizer = new HtmlSanitizer(new HtmlSanitizerOptions {
            AllowedTags = new HashSet<string>(),
            AllowedAttributes = new HashSet<string>(),
            AllowedSchemes = new HashSet<string>(),
            AllowedCssProperties = new HashSet<string>(),
            UriAttributes = new HashSet<string>()
        });
        sanitizer.RemovingTag += MarkDisallowed;
        sanitizer.RemovingComment += MarkDisallowed;

        var document = sanitizer.SanitizeDom(text);

        //see the identical guard in ContainsDisallowedRichText - a literal <html>/<head>/<body> tag merges
        //into the document root and its own attributes never reach RemovingAttribute
        if (HasOwnAttributes(document.DocumentElement) || HasOwnAttributes(document.Head) ||
            HasOwnAttributes(document.Body))
            disallowedContentSeen = true;

        return disallowedContentSeen;
    }

    private static bool HasOwnAttributes(AngleSharp.Dom.IElement element)
    {
        return element is not null && element.Attributes.Length > 0;
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

    private static bool IsAllowedIframeUrl(string url, string[] allowedIframeHosts)
    {
        if (allowedIframeHosts.Length == 0) return false;

        //protocol-relative ("//host/path") is parsed as a relative Uri by .NET - IsAbsoluteUri is false - but a
        //browser resolves it against the current page's scheme, i.e. as an absolute url to an arbitrary host.
        //Reject it before the relative-url fast path below would otherwise wave it through as same-origin.
        if (url.StartsWith("//", StringComparison.Ordinal)) return false;

        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri)) return false;

        //a genuinely relative url (no leading "//") cannot leave this origin - it is how the file manager
        //inserts self-hosted video
        if (!uri.IsAbsoluteUri) return true;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;

        return allowedIframeHosts.Any(allowed => IsHostMatch(uri.Host, allowed));
    }

    private static bool IsHostMatch(string host, string allowed)
    {
        if (allowed.StartsWith("*.", StringComparison.Ordinal))
            return host.EndsWith(allowed[1..], StringComparison.OrdinalIgnoreCase);

        return string.Equals(host, allowed, StringComparison.OrdinalIgnoreCase);
    }
}
