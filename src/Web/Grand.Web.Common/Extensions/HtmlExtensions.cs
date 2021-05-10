using Microsoft.AspNetCore.Html;
using System.IO;
using System.Text.Encodings.Web;

namespace Grand.Web.Common.Extensions
{
    public static class HtmlExtensions
    {
        #region Admin area extensions

        public static string RenderHtmlContent(this IHtmlContent htmlContent)
        {
            using (var writer = new StringWriter())
            {
                htmlContent.WriteTo(writer, HtmlEncoder.Default);
                var htmlOutput = writer.ToString();
                return htmlOutput;
            }
        }
        #endregion

        #region Common extensions

        public static string ToHtmlString(this IHtmlContent tag)
        {
            using (var writer = new StringWriter())
            {
                tag.WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }
        }

        #endregion
    }
}

