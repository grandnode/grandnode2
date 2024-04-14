using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers;

[HtmlTargetElement("textarea", Attributes = ForAttributeName)]
public class GrandTextAreaTagHelper : TextAreaTagHelper
{
    private const string ForAttributeName = "asp-for";

    public GrandTextAreaTagHelper(IHtmlGenerator generator) : base(generator)
    {
    }

    [HtmlAttributeName("asp-disabled")] public string IsDisabled { set; get; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        bool.TryParse(IsDisabled, out var disabled);
        if (disabled)
        {
            var d = new TagHelperAttribute("disabled", "disabled");
            output.Attributes.Add(d);
        }

        base.Process(context, output);
    }
}