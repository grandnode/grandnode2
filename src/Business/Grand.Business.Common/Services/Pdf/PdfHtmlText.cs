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

        var text = Hidden().Replace(html, string.Empty);
        text = LineBreak().Replace(text, "\n");
        text = CellEnd().Replace(text, " ");
        text = Tag().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text).Replace(' ', ' ');

        var lines = text.Split('\n')
            .Select(l => Whitespace().Replace(l, " ").Trim())
            .Where(l => l.Length > 0);
        return string.Join("\n", lines);
    }

    //content that is never shown on a page
    [GeneratedRegex(@"<(script|style)\b[^>]*>.*?</\1\s*>|<!--.*?-->", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex Hidden();

    [GeneratedRegex(@"<(br|hr)\b[^>]*>|</(p|div|li|tr|table|ul|ol|h[1-6])\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreak();

    //table cells next to each other keep a space between their texts
    [GeneratedRegex(@"</t[dh]\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex CellEnd();

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"[ \t\r\f\v]+")]
    private static partial Regex Whitespace();
}
