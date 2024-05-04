using Grand.Web.Common.Page;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers;

[HtmlTargetElement("html-class", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("html-class", Attributes = ForAttributeName)]
public class HtmlClassTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-name";

    private readonly IPageHeadBuilder _pageHeadBuilder;

    public HtmlClassTagHelper(IPageHeadBuilder pageHeadBuilder)
    {
        _pageHeadBuilder = pageHeadBuilder;
    }

    [HtmlAttributeName(ForAttributeName)] public string Part { set; get; }

    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.SuppressOutput();
        if (!string.IsNullOrEmpty(Part)) _pageHeadBuilder.AddPageCssClassParts(Part);
        return Task.CompletedTask;
    }
}