using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers.Admin;

[HtmlTargetElement("tabstrip-item")]
public class AdminTabStripItemTagHelper : TagHelper
{
    [ViewContext] public ViewContext ViewContext { get; set; }

    [HtmlAttributeName("Text")] public string Text { get; set; }

    [HtmlAttributeName("tab-index")] public int CurrentIndex { set; get; }

    private int GetSelectedTabIndex()
    {
        var index = 0;
        var dataKey = "Grand.selected-tab-index";
        if (ViewContext.ViewData[dataKey] is int) index = (int)ViewContext.ViewData[dataKey];
        if (ViewContext.TempData[dataKey] is int) index = (int)ViewContext.TempData[dataKey];

        if (index < 0)
            index = 0;

        return index;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        _ = await output.GetChildContentAsync();
        output.TagName = "li";

        var selected = GetSelectedTabIndex() == CurrentIndex;
        //a view can force the active tab of a nested strip with class="k-state-active"
        //(Product/ProductAttributes), so the attribute is merged, never replaced
        var classes = output.Attributes["class"]?.Value?.ToString() ?? string.Empty;
        var css = new List<string> { "nav-item" };
        if (!string.IsNullOrWhiteSpace(classes)) css.Add(classes.Trim());
        if (selected && !classes.Contains("k-state-active", StringComparison.Ordinal))
            //k-state-active is kept next to the Bootstrap classes: admin.common.js looks it up
            css.Add("active k-state-active");
        output.Attributes.SetAttribute("class", string.Join(' ', css));
        output.Attributes.SetAttribute("role", "presentation");
        selected = css.Contains("active k-state-active") || classes.Contains("k-state-active", StringComparison.Ordinal);

        var link = new TagBuilder("a");
        link.AddCssClass("nav-link");
        if (selected) link.AddCssClass("active");
        link.Attributes["href"] = "#";
        link.Attributes["role"] = "tab";
        link.Attributes["aria-selected"] = selected ? "true" : "false";
        link.Attributes["tabindex"] = selected ? "0" : "-1";
        //Text is view-authored (a localized resource), rendered as it was before
        link.InnerHtml.AppendHtml(Text);
        output.Content.AppendHtml(link);
    }
}
