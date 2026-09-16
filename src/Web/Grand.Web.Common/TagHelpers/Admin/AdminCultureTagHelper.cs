using Grand.Web.Common.TagHelpers.Admin.Grid;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     Renders the number and date formatting data of the request culture once per page, as
///     the JSON island admin.ui.js reads (adminapp/src/ui/culture.js). It replaces the
///     kendo.culture("xx-XX") call the three panel layouts made and the culture script the
///     Head partials loaded, and it is the same shape the &lt;admin-grid&gt; tag helper puts
///     in data-grand-grid, so the widgets and the grids format alike.
/// </summary>
[HtmlTargetElement("admin-culture", TagStructure = TagStructure.WithoutEndTag)]
public class AdminCultureTagHelper : TagHelper
{
    public const string ElementId = "grand-admin-culture";

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        //the JSON is the text of a <script> element, so < > & must not be left as they are
        Encoder = JavaScriptEncoder.Default
    };

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "script";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("type", "application/json");
        output.Attributes.SetAttribute("id", ElementId);
        output.Content.SetHtmlContent(
            JsonSerializer.Serialize(GridCulture.From(CultureInfo.CurrentCulture), JsonOptions));
    }
}
