using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     The shell of a secondary form - what opens over a screen rather than as one: the picture
///     editor, a tier price, an attribute value, a product picker. It replaces the
///     <c>.row &gt; .col-md-12 &gt; .x_panel &gt; .x_title &gt; .caption</c> chrome each popup
///     built by hand.
///     It carries <c>grand-page</c> as well as <c>grand-popup</c>, so a popup gets the buttons,
///     fields and grids of a converted page without a second copy of every one of those rules;
///     <c>grand-popup</c> only sizes the title and lays out the action row
///     (<c>.grand-popup__actions</c>, written by the view).
/// </summary>
[HtmlTargetElement("admin-popup")]
public class AdminPopupTagHelper : TagHelper
{
    [HtmlAttributeName("title")] public string Title { get; set; }

    /// <summary>A bootstrap-icons name ("bi-image") shown before the title.</summary>
    [HtmlAttributeName("icon")]
    public string Icon { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var content = await output.GetChildContentAsync();

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("grand-page", HtmlEncoder.Default);
        output.AddClass("grand-popup", HtmlEncoder.Default);

        if (!string.IsNullOrEmpty(Title))
        {
            var title = new TagBuilder("h2");
            title.AddCssClass("grand-popup__title");
            if (!string.IsNullOrEmpty(Icon))
            {
                var icon = new TagBuilder("i");
                icon.AddCssClass("bi");
                icon.AddCssClass(Icon);
                title.InnerHtml.AppendHtml(icon);
            }

            title.InnerHtml.Append(Title);
            output.PreContent.AppendHtml(title);
        }

        output.Content.SetHtmlContent(content);
    }
}
