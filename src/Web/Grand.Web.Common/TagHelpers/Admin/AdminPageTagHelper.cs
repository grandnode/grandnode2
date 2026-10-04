using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     The shell of an admin screen: a page header (title, optional icon, subtitle, back link and
///     the action cluster) above a column of cards, in place of the
///     <c>.row &gt; .col-md-12 &gt; .x_panel.light.form-fit &gt; .x_title</c> chrome every view
///     used to assemble by hand. Styled by <c>_components.scss</c> (.grand-page).
///     The actions go in a child &lt;page-actions&gt; element.
/// </summary>
[HtmlTargetElement("admin-page")]
public class AdminPageTagHelper : TagHelper
{
    internal static readonly object ActionsKey = typeof(PageActionsTagHelper);

    /// <summary>The page title. Localized by the view, like every other text.</summary>
    [HtmlAttributeName("title")]
    public string Title { get; set; }

    /// <summary>A bootstrap-icons name ("bi-boxes") shown before the title.</summary>
    [HtmlAttributeName("icon")]
    public string Icon { get; set; }

    /// <summary>What the screen is about: the entity being edited, a count, a store name.</summary>
    [HtmlAttributeName("meta")]
    public string Meta { get; set; }

    /// <summary>Href of the back link; the view builds it with Url.Action.</summary>
    [HtmlAttributeName("back-url")]
    public string BackUrl { get; set; }

    [HtmlAttributeName("back-text")] public string BackText { get; set; }

    /// <summary>
    ///     Keeps the header and the tab strip in view while a long tab scrolls, so the one primary
    ///     action stays reachable. For detail screens.
    /// </summary>
    [HtmlAttributeName("sticky")]
    public bool Sticky { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var actions = new DefaultTagHelperContent();
        context.Items[ActionsKey] = actions;
        var body = await output.GetChildContentAsync();

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("grand-page", System.Text.Encodings.Web.HtmlEncoder.Default);
        if (Sticky) output.AddClass("grand-page--sticky", System.Text.Encodings.Web.HtmlEncoder.Default);

        output.PreContent.AppendHtml(Header(actions));
        output.Content.SetHtmlContent(body);
    }

    private IHtmlContent Header(IHtmlContent actions)
    {
        var header = new TagBuilder("div");
        header.AddCssClass("grand-page__header");

        var heading = new TagBuilder("div");
        heading.AddCssClass("grand-page__heading");

        var title = new TagBuilder("h1");
        title.AddCssClass("grand-page__title");
        if (!string.IsNullOrEmpty(Icon))
        {
            var icon = new TagBuilder("i");
            icon.AddCssClass("bi");
            icon.AddCssClass(Icon);
            title.InnerHtml.AppendHtml(icon);
        }

        title.InnerHtml.Append(Title ?? string.Empty);
        heading.InnerHtml.AppendHtml(title);

        if (!string.IsNullOrEmpty(Meta))
        {
            var meta = new TagBuilder("p");
            meta.AddCssClass("grand-page__meta");
            meta.InnerHtml.Append(Meta);
            heading.InnerHtml.AppendHtml(meta);
        }

        if (!string.IsNullOrEmpty(BackUrl))
        {
            var back = new TagBuilder("a");
            back.AddCssClass("grand-page__back");
            back.Attributes["href"] = BackUrl;
            var arrow = new TagBuilder("i");
            arrow.AddCssClass("bi");
            arrow.AddCssClass("bi-arrow-left");
            back.InnerHtml.AppendHtml(arrow);
            back.InnerHtml.Append(BackText ?? string.Empty);
            heading.InnerHtml.AppendHtml(back);
        }

        header.InnerHtml.AppendHtml(heading);

        var cluster = new TagBuilder("div");
        cluster.AddCssClass("grand-page__actions");
        cluster.InnerHtml.AppendHtml(actions);
        header.InnerHtml.AppendHtml(cluster);
        return header;
    }
}

/// <summary>
///     The action cluster of a page header. Exactly one of its buttons carries
///     <c>btn-primary</c>; the rest are <c>btn-outline-secondary</c>, and a destructive action
///     belongs in an &lt;admin-action-menu&gt;.
/// </summary>
[HtmlTargetElement("page-actions", ParentTag = "admin-page")]
public class PageActionsTagHelper : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var content = await output.GetChildContentAsync();
        if (context.Items.TryGetValue(AdminPageTagHelper.ActionsKey, out var target) &&
            target is TagHelperContent actions)
            actions.AppendHtml(content);

        //the cluster the page header renders is the only place this content appears
        output.SuppressOutput();
    }
}
