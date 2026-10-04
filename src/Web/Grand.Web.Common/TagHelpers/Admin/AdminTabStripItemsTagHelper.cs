using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers.Admin;

[HtmlTargetElement("items", ParentTag = "admin-tabstrip")]
public class AdminTabStripItemsTagHelper : TagHelper
{
    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "ul";
        //the class names Bootstrap 4 and 5 share, so the panel theme styles the tabs
        output.Attributes.SetAttribute("class", "nav nav-tabs");
        output.Attributes.SetAttribute("role", "tablist");
        return Task.CompletedTask;
    }
}
