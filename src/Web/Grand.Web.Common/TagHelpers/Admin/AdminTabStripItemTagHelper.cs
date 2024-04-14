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

        var selectedIndex = GetSelectedTabIndex();
        if (selectedIndex == CurrentIndex) output.Attributes.SetAttribute("class", "k-state-active");

        output.Content.AppendHtml(Text);
    }
}