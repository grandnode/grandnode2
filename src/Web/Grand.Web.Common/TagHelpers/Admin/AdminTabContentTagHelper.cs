using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers.Admin;

[HtmlTargetElement("content", ParentTag = "tabstrip-item")]
public class AdminTabContentTagHelper : TagHelper
{
    //the view data key of the contents the strip collects: the type's full name
    internal const string ContentsKey = "Grand.Web.Common.TagHelpers.Admin.AdminTabContentTagHelper";

    //set by the framework before the tag helper runs
    [ViewContext] public ViewContext ViewContext { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = await output.GetChildContentAsync();
        //outside a tab strip there is no list to collect the content in
        if (ViewContext.ViewData[ContentsKey] is List<string> list)
            list.Add(content.GetContent());
        output.SuppressOutput();
    }
}