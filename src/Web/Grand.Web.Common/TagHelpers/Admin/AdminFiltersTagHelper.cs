using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     The filter bar of a list screen: one search field that is always visible, a Filters toggle,
///     and the rest of the criteria in a two-column panel that stays collapsed until asked for.
///     It replaces <c>.main-header</c> + <c>.drop-filters-container</c> and the
///     <c>form-horizontal</c> column of half-width selects the list views stacked inside a
///     collapse. The child elements are the advanced fields, normally &lt;admin-field&gt;.
/// </summary>
[HtmlTargetElement("admin-filters")]
public class AdminFiltersTagHelper : TagHelper
{
    internal static readonly object SearchKey = typeof(FiltersSearchTagHelper);

    private readonly IContextAccessor _contextAccessor;
    private readonly ITranslationService _translationService;

    public AdminFiltersTagHelper(IContextAccessor contextAccessor, ITranslationService translationService)
    {
        _contextAccessor = contextAccessor;
        _translationService = translationService;
    }

    /// <summary>Id of the element the bar is built around; the advanced panel derives its own.</summary>
    [HtmlAttributeName("id")]
    public string Id { get; set; } = "grand-filters";

    /// <summary>The id the search button gets, so a view's script can click it.</summary>
    [HtmlAttributeName("submit-id")]
    public string SubmitId { get; set; }

    /// <summary>Opens the advanced panel on load, for a screen arriving with filters applied.</summary>
    [HtmlAttributeName("expanded")]
    public bool Expanded { get; set; }

    /// <summary>
    ///     Renders Search as an outline button, for a screen whose one primary action is elsewhere -
    ///     a batch grid's Save changes, say.
    /// </summary>
    [HtmlAttributeName("secondary-submit")]
    public bool SecondarySubmit { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var search = new DefaultTagHelperContent();
        context.Items[SearchKey] = search;
        var advanced = await output.GetChildContentAsync();
        var advancedId = $"{Id}-advanced";

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("id", Id);
        output.AddClass("grand-filters", HtmlEncoder.Default);

        var quick = new TagBuilder("div");
        quick.AddCssClass("grand-filters__quick");

        if (!search.IsEmptyOrWhiteSpace)
        {
            var searchElement = new TagBuilder("div");
            searchElement.AddCssClass("grand-filters__search");
            searchElement.InnerHtml.AppendHtml(search);
            quick.InnerHtml.AppendHtml(searchElement);
        }

        if (!advanced.IsEmptyOrWhiteSpace)
        {
            var toggle = new TagBuilder("button");
            toggle.Attributes["type"] = "button";
            toggle.AddCssClass("btn btn-outline-secondary");
            toggle.Attributes["data-bs-toggle"] = "collapse";
            toggle.Attributes["data-bs-target"] = $"#{advancedId}";
            toggle.Attributes["aria-expanded"] = Expanded ? "true" : "false";
            toggle.Attributes["aria-controls"] = advancedId;
            toggle.InnerHtml.AppendHtml("<i class=\"bi bi-funnel\"></i> ");
            toggle.InnerHtml.Append(Text("Admin.Common.Filters", "Filters"));
            quick.InnerHtml.AppendHtml(toggle);
        }

        var submit = new TagBuilder("button");
        submit.Attributes["type"] = "submit";
        if (!string.IsNullOrEmpty(SubmitId)) submit.Attributes["id"] = SubmitId;
        submit.AddCssClass(SecondarySubmit ? "btn btn-outline-secondary" : "btn btn-primary");
        submit.InnerHtml.AppendHtml("<i class=\"bi bi-search\"></i> ");
        submit.InnerHtml.Append(Text("Admin.Common.Search", "Search"));
        quick.InnerHtml.AppendHtml(submit);

        output.Content.AppendHtml(quick);

        if (advanced.IsEmptyOrWhiteSpace) return;

        var panel = new TagBuilder("div");
        panel.Attributes["id"] = advancedId;
        panel.AddCssClass(Expanded ? "collapse show grand-filters__advanced" : "collapse grand-filters__advanced");
        var fields = new TagBuilder("div");
        fields.AddCssClass("grand-fields grand-fields--2col");
        fields.InnerHtml.AppendHtml(advanced);
        panel.InnerHtml.AppendHtml(fields);
        output.Content.AppendHtml(panel);
    }

    private string Text(string key, string fallback)
    {
        return AdminText.Resource(_translationService, _contextAccessor, key, fallback);
    }
}

/// <summary>
///     The always-visible search control of a filter bar: whatever the list searches by first,
///     usually the name. Everything else belongs in the collapsed panel.
/// </summary>
[HtmlTargetElement("filters-search", ParentTag = "admin-filters")]
public class FiltersSearchTagHelper : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var content = await output.GetChildContentAsync();
        if (context.Items.TryGetValue(AdminFiltersTagHelper.SearchKey, out var target) &&
            target is TagHelperContent search)
            search.AppendHtml(content);

        //the quick row of the bar is the only place this content appears
        output.SuppressOutput();
    }
}
