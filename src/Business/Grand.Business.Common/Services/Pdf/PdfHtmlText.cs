using System.Net;
using System.Text.RegularExpressions;

namespace Grand.Business.Common.Services.Pdf;

/// <summary>
///     Turns the HTML fragments that reach a PDF (attribute descriptions joined with &lt;br /&gt;, the invoice
///     header and footer from PDF settings) into plain text lines. Markup is dropped, never rendered.
/// </summary>
internal static partial class PdfHtmlText
{
    public static string ToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var text = LineBreak().Replace(html, "\n");
        text = Tag().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text).Replace(' ', ' ');

        var lines = text.Split('\n')
            .Select(l => Whitespace().Replace(l, " ").Trim())
            .Where(l => l.Length > 0);
        return string.Join("\n", lines);
    }

    [GeneratedRegex(@"<br\s*/?>|</(p|div|li|tr|h[1-6])\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreak();

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"[ \t\r\f\v]+")]
    private static partial Regex Whitespace();
}
