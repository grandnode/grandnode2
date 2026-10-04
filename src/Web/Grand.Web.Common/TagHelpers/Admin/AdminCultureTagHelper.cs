using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
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
///     It also carries the handful of texts the date picker draws, so its buttons are in the
///     panel's working language rather than the browser's.
/// </summary>
[HtmlTargetElement("admin-culture", TagStructure = TagStructure.WithoutEndTag)]
public class AdminCultureTagHelper : TagHelper
{
    public const string ElementId = "grand-admin-culture";

    /// <summary>Texts the widgets show, by the name admin.ui.js reads them under.</summary>
    internal static readonly IReadOnlyDictionary<string, string> TextResources =
        new Dictionary<string, string> {
            ["today"] = "Admin.Common.Today",
            ["clear"] = "Admin.Common.Clear",
            ["cancel"] = "Admin.Common.Cancel",
            ["previousMonth"] = "Admin.Common.Calendar.PreviousMonth",
            ["nextMonth"] = "Admin.Common.Calendar.NextMonth",
            ["openCalendar"] = "Admin.Common.Calendar.Open",
            ["noRecords"] = "Admin.Common.Grid.NoRecords",
            ["select"] = "Admin.Common.Select"
        };

    private readonly IContextAccessor _contextAccessor;
    private readonly ITranslationService _translationService;

    public AdminCultureTagHelper(ITranslationService translationService, IContextAccessor contextAccessor)
    {
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

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
        var culture = GridCulture.From(CultureInfo.CurrentCulture);
        output.Content.SetHtmlContent(JsonSerializer.Serialize(new {
            culture.Name,
            culture.NumberFormat,
            culture.Calendar,
            Texts = Texts()
        }, JsonOptions));
    }

    /// <summary>
    ///     A resource missing from the database (an installation that never imported the
    ///     upgrade file) is left out, so the widget keeps its own neutral default instead of
    ///     showing the raw resource key.
    /// </summary>
    private IDictionary<string, string> Texts()
    {
        var languageId = _contextAccessor.WorkContext?.WorkingLanguage?.Id;
        var texts = new Dictionary<string, string>();
        if (languageId is null) return texts;
        foreach (var (name, key) in TextResources)
        {
            var value = _translationService.GetResource(key, languageId, string.Empty, true);
            if (!string.IsNullOrEmpty(value)) texts[name] = value;
        }

        return texts;
    }
}
