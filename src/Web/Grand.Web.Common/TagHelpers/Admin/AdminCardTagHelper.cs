using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     A titled section of a screen: the card of the dashboard, now available everywhere.
///     It replaces <c>x_content</c> and the <c>note note-info</c> bar the views used as a
///     section heading - a message component doing a heading's job. Styled by
///     <c>_dashboard.scss</c> (.grand-card) and <c>_components.scss</c>.
/// </summary>
[HtmlTargetElement("admin-card")]
public class AdminCardTagHelper : TagHelper
{
    [HtmlAttributeName("title")] public string Title { get; set; }

    /// <summary>A bootstrap-icons name ("bi-box-seam") shown before the title.</summary>
    [HtmlAttributeName("icon")]
    public string Icon { get; set; }

    /// <summary>A link at the end of the header ("see all"), with <see cref="ActionText" />.</summary>
    [HtmlAttributeName("action-url")]
    public string ActionUrl { get; set; }

    [HtmlAttributeName("action-text")] public string ActionText { get; set; }

    /// <summary>Drops the body padding, for a card holding nothing but a grid.</summary>
    [HtmlAttributeName("flush")]
    public bool Flush { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var content = await output.GetChildContentAsync();

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("grand-card", System.Text.Encodings.Web.HtmlEncoder.Default);

        if (!string.IsNullOrEmpty(Title))
        {
            var header = new TagBuilder("div");
            header.AddCssClass("grand-card__header");
            if (!string.IsNullOrEmpty(Icon))
            {
                var icon = new TagBuilder("i");
                icon.AddCssClass("bi");
                icon.AddCssClass(Icon);
                icon.AddCssClass("grand-card__icon");
                header.InnerHtml.AppendHtml(icon);
            }

            var title = new TagBuilder("h2");
            title.AddCssClass("grand-card__title");
            title.InnerHtml.Append(Title);
            header.InnerHtml.AppendHtml(title);

            if (!string.IsNullOrEmpty(ActionUrl))
            {
                var action = new TagBuilder("a");
                action.AddCssClass("grand-card__action");
                action.Attributes["href"] = ActionUrl;
                action.InnerHtml.Append(ActionText ?? string.Empty);
                header.InnerHtml.AppendHtml(action);
            }

            output.PreContent.AppendHtml(header);
        }

        var body = new TagBuilder("div");
        body.AddCssClass("grand-card__body");
        if (Flush) body.AddCssClass("grand-card__body--flush");
        body.InnerHtml.AppendHtml(content);
        output.Content.SetHtmlContent(body);
    }
}
