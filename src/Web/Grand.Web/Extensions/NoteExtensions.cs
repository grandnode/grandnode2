using Grand.Infrastructure.Security;
using Grand.SharedKernel.Extensions;

namespace Grand.Web.Extensions;

public static class NoteExtensions
{
    /// <summary>
    ///     What the customer typed is text, as the e-mails and the panels show it. A staff note is text too, unless it
    ///     is markup the rich-text allowlist keeps whole, e.g. a link to a form: such notes were shown as HTML before
    /// </summary>
    public static string NoteHtml(this IHtmlSanitizationService htmlSanitizationService, string note,
        bool createdByCustomer)
    {
        if (!createdByCustomer && htmlSanitizationService.ContainsMarkup(note) &&
            !htmlSanitizationService.ContainsDisallowedRichText(note))
            return note;

        return FormatText.ConvertText(note);
    }
}
