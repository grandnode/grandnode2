using Grand.Infrastructure.Security;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Encodings.Web;

namespace Grand.Web.Common.Extensions;

public static class HtmlExtensions
{
    #region Sanitized output

    /// <summary>
    ///     Writes stored rich text without encoding it, after sanitizing it against an allowlist.
    ///     Use this instead of <c>Html.Raw</c> for anything an editor produced: content saved before sanitization
    ///     was applied on write is still in the database, and a description written by a vendor or a store manager
    ///     is rendered back into the administration panel.
    ///     Do not use it on strings the server already encoded (formatted attributes, addresses, contact enquiries)
    ///     - those would be double-encoded.
    /// </summary>
    public static IHtmlContent RawSanitized(this IHtmlHelper helper, string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return HtmlString.Empty;

        var sanitizationService = helper.ViewContext.HttpContext.RequestServices
            .GetRequiredService<IHtmlSanitizationService>();

        return new HtmlString(sanitizationService.SanitizeRichText(html));
    }

    #endregion

    #region Admin area extensions

    public static string RenderHtmlContent(this IHtmlContent htmlContent)
    {
        using var writer = new StringWriter();
        htmlContent.WriteTo(writer, HtmlEncoder.Default);
        var htmlOutput = writer.ToString();
        return htmlOutput;
    }

    #endregion

    #region Common extensions

    public static string ToHtmlString(this IHtmlContent tag)
    {
        using var writer = new StringWriter();
        tag.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
    public static string HtmlEncodeSafe(string input)
    {
        return string.IsNullOrEmpty(input) ? string.Empty : WebUtility.HtmlEncode(input);
    }

    #endregion
}