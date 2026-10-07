using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     A multi-line text field bound to a model property: a &lt;textarea&gt; with the panel's
///     form-control styling, a default size of 4 rows by 20 columns, and the required marker
///     when asked for.
/// </summary>
[HtmlTargetElement("admin-textarea", Attributes = ForAttributeName)]
public class AdminTextAreaTagHelper : TextAreaTagHelper
{
    private const string ForAttributeName = "asp-for";
    private const string RequiredAttributeName = "asp-required";
    private const string DisabledAttributeName = "asp-disabled";

    public AdminTextAreaTagHelper(IHtmlGenerator generator) : base(generator)
    {
    }

    /// <summary>"true" renders the field disabled, so the browser does not post it.</summary>
    [HtmlAttributeName(DisabledAttributeName)]
    public string IsDisabled { set; get; }

    /// <summary>"true" wraps the field in the input group that shows the required asterisk.</summary>
    [HtmlAttributeName(RequiredAttributeName)]
    public string IsRequired { set; get; }

    /// <summary>
    ///     Keeps a class set in the view instead of the default form-control. An explicit
    ///     rows or cols attribute wins over the default size.
    /// </summary>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        //tag details
        output.TagName = "textarea";
        output.TagMode = TagMode.StartTagAndEndTag;

        //merge classes
        var classValue = output.Attributes.ContainsName("class")
            ? $"{output.Attributes["class"].Value}"
            : "form-control";
        output.Attributes.SetAttribute("class", classValue);

        //add disabled attribute
        bool.TryParse(IsDisabled, out var disabled);
        if (disabled)
        {
            var d = new TagHelperAttribute("disabled", "disabled");
            output.Attributes.Add(d);
        }

        //additional parameters
        var rowsNumber = output.Attributes.ContainsName("rows") ? output.Attributes["rows"].Value : 4;
        output.Attributes.SetAttribute("rows", rowsNumber);
        var colsNumber = output.Attributes.ContainsName("cols") ? output.Attributes["cols"].Value : 20;
        output.Attributes.SetAttribute("cols", colsNumber);

        //required asterisk
        bool.TryParse(IsRequired, out var required);
        if (required)
        {
            output.PreElement.SetHtmlContent("<div class='input-group input-group-required'>");
            output.PostElement.SetHtmlContent(
                "<div class=\"input-group-btn\"><span class=\"required\">*</span></div></div>");
        }

        base.Process(context, output);
    }
}