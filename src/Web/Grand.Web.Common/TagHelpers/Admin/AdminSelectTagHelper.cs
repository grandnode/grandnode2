using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers.Admin;

[HtmlTargetElement("admin-select", Attributes = ForAttributeName)]
public class AdminSelectTagHelper : SelectTagHelper
{
    private const string ForAttributeName = "asp-for";
    private const string DisplayHintAttributeName = "asp-display-hint";

    public AdminSelectTagHelper(IHtmlGenerator generator) : base(generator)
    {
    }

    [HtmlAttributeName(DisplayHintAttributeName)]
    public bool DisplayHint { get; set; } = true;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        await base.ProcessAsync(context, output);
        output.TagName = "select";
        output.TagMode = TagMode.StartTagAndEndTag;
        //Bootstrap 5 styles a <select> with form-select; form-control resets its appearance
        //and takes the native arrow away
        var classValue = "form-select ";
        if (context.AllAttributes.TryGetAttribute("class", out var forAttribute))
            classValue += forAttribute.Value.ToString();
        output.Attributes.SetAttribute("class", classValue);
    }
}