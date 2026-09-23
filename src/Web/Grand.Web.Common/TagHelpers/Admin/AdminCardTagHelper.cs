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
    internal static readonly object ActionsKey = typeof(CardActionsTagHelper);

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

        var actions = new DefaultTagHelperContent();
        context.Items[ActionsKey] = actions;
        var content = await output.GetChildContentAsync();

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("grand-card", System.Text.Encodings.Web.HtmlEncoder.Default);

        //a card with actions gets a header even without a title, so "Add new" always sits in the
        //same place - the top right of the card - whichever grid it belongs to
        var hasActions = !actions.IsEmptyOrWhiteSpace;
        if (!string.IsNullOrEmpty(Title) || hasActions)
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

            //an untitled header still needs the growing title slot, or the actions would sit left
            var title = new TagBuilder("h2");
            title.AddCssClass("grand-card__title");
            title.InnerHtml.Append(Title ?? string.Empty);
            header.InnerHtml.AppendHtml(title);

            if (!string.IsNullOrEmpty(ActionUrl))
            {
                var action = new TagBuilder("a");
                action.AddCssClass("grand-card__action");
                action.Attributes["href"] = ActionUrl;
                action.InnerHtml.Append(ActionText ?? string.Empty);
                header.InnerHtml.AppendHtml(action);
            }

            if (hasActions)
            {
                var cluster = new TagBuilder("div");
                cluster.AddCssClass("grand-card__actions");
                cluster.InnerHtml.AppendHtml(actions);
                header.InnerHtml.AppendHtml(cluster);
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

/// <summary>
///     The buttons of a card's header - "Add new" for the grid the card holds, first of all.
///     Every grid on a screen puts its add button here, top right, rather than some in the
///     grid's toolbar and some under the grid.
/// </summary>
[HtmlTargetElement("card-actions", ParentTag = "admin-card")]
public class CardActionsTagHelper : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var content = await output.GetChildContentAsync();
        if (context.Items.TryGetValue(AdminCardTagHelper.ActionsKey, out var target) &&
            target is TagHelperContent actions)
            actions.AppendHtml(content);

        //the card's header is the only place this content appears
        output.SuppressOutput();
    }
}
