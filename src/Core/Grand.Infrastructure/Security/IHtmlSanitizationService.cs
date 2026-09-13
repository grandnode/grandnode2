namespace Grand.Infrastructure.Security;

/// <summary>
///     Detects markup that falls outside an allowlist. Content edited by a vendor, a store manager, or any
///     non-superadmin account is untrusted: it is rendered back into the administration panel, so markup that
///     survives here executes in an administrator's session.
///     This is detection, not rewriting: callers reject the value (see <see cref="Grand.Infrastructure.Validators.SanitizeHtmlAttribute" />
///     / <see cref="Grand.Infrastructure.Validators.NoHtmlAttribute" />) rather than silently cleaning it, so a
///     rejected save always shows the editor what needs to change.
/// </summary>
public interface IHtmlSanitizationService
{
    /// <summary>
    ///     True when the rich-text allowlist would remove something from <paramref name="html" /> - a tag,
    ///     attribute, style rule, class, comment, or url (script, iframe to an unlisted host, javascript:, etc.)
    ///     that can execute or navigate on its own. False for markup an editor legitimately produces, even if the
    ///     allowlist would reformat it (e.g. an implied &lt;tbody&gt; made explicit).
    /// </summary>
    /// <param name="html">Untrusted markup; may be null</param>
    bool ContainsDisallowedRichText(string html);

    /// <summary>
    ///     True when <paramref name="text" /> contains any HTML markup at all. For fields that are never rich
    ///     text: meta titles, meta keywords, meta descriptions, admin comments, attribute names.
    /// </summary>
    /// <param name="text">Untrusted text; may be null</param>
    bool ContainsMarkup(string text);
}
