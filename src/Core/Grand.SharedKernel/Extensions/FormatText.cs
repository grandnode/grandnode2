using System.Net;

namespace Grand.SharedKernel.Extensions;

public static class FormatText
{
    public static string ConvertText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        text = WebUtility.HtmlEncode(text);

        text = text.Replace("\r\n", "<br />");
        text = text.Replace("\t", "&nbsp;&nbsp;");

        return text;
    }
}