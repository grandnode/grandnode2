using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     What can be done to the rows a list has ticked, shown only once some are.
///     The panels used to keep those buttons next to "Add new", where they read as page actions
///     and did nothing until a row was selected. Driven by the grid's own
///     <c>grand-grid:selection</c> event through adminapp/src/ui/bulkbar.js; the child elements
///     are the buttons.
/// </summary>
[HtmlTargetElement("admin-bulkbar", Attributes = "grid-id")]
public class AdminBulkBarTagHelper : TagHelper
{
    private readonly IContextAccessor _contextAccessor;
    private readonly ITranslationService _translationService;

    public AdminBulkBarTagHelper(IContextAccessor contextAccessor, ITranslationService translationService)
    {
        _contextAccessor = contextAccessor;
        _translationService = translationService;
    }

    /// <summary>Id of the grid whose selection drives the bar.</summary>
    [HtmlAttributeName("grid-id")]
    public string GridId { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var actions = await output.GetChildContentAsync();

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("grand-bulkbar", HtmlEncoder.Default);
        output.Attributes.SetAttribute("data-grand-bulkbar", GridId);

        var count = new TagBuilder("span");
        count.AddCssClass("grand-bulkbar__count");
        //the runtime fills the text with the count in place of {0}
        count.Attributes["data-bulkbar-count"] =
            AdminText.Resource(_translationService, _contextAccessor, "Admin.Common.Grid.SelectedCount",
                "{0} selected");
        output.PreContent.AppendHtml(count);
        output.Content.SetHtmlContent(actions);
    }
}
