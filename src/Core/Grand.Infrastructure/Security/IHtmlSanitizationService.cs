namespace Grand.Infrastructure.Security;

/// <summary>
///     Sanitizes untrusted markup against an allowlist.
///     Content edited by a vendor, a store manager, or any non-superadmin account is untrusted: it is rendered
///     back into the administration panel, so markup that survives here executes in an administrator's session.
/// </summary>
public interface IHtmlSanitizationService
{
    /// <summary>
    ///     Sanitizes rich-text markup (product descriptions, blog posts, page bodies), keeping the formatting an
    ///     editor legitimately produces and removing everything that can execute or navigate on its own.
    /// </summary>
    /// <param name="html">Untrusted markup; may be null</param>
    /// <returns>Markup safe to write into a page, or the input unchanged when it is null or empty</returns>
    string SanitizeRichText(string html);

    /// <summary>
    ///     Removes all markup and returns the plain text it contained. For fields that are never rich text:
    ///     meta titles, meta keywords, meta descriptions, admin comments, attribute names.
    /// </summary>
    /// <param name="text">Untrusted text; may be null</param>
    /// <returns>The text with every tag removed and entities decoded</returns>
    string StripHtml(string text);
}
