using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     The overflow menu of a page header: the actions a screen offers but should not put in
///     front of the primary one - export, import, copy, and anything destructive. It is what
///     keeps Delete from sitting one pixel away from Save. Bootstrap dropdown markup; the child
///     elements are &lt;admin-action&gt;.
/// </summary>
[HtmlTargetElement("admin-action-menu")]
[RestrictChildren("admin-action")]
public class AdminActionMenuTagHelper : TagHelper
{
    private readonly IContextAccessor _contextAccessor;
    private readonly ITranslationService _translationService;

    public AdminActionMenuTagHelper(IContextAccessor contextAccessor, ITranslationService translationService)
    {
        _contextAccessor = contextAccessor;
        _translationService = translationService;
    }

    /// <summary>The trigger's text. Empty leaves the icon alone, as an overflow button.</summary>
    [HtmlAttributeName("text")]
    public string Text { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var items = await output.GetChildContentAsync();

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("dropdown", HtmlEncoder.Default);

        var toggle = new TagBuilder("button");
        toggle.Attributes["type"] = "button";
        toggle.AddCssClass("btn btn-outline-secondary");
        toggle.Attributes["data-bs-toggle"] = "dropdown";
        toggle.Attributes["aria-expanded"] = "false";
        toggle.Attributes["aria-label"] =
            AdminText.Resource(_translationService, _contextAccessor, "Admin.Common.Actions", "Actions");
        toggle.InnerHtml.AppendHtml("<i class=\"bi bi-three-dots-vertical\"></i>");
        if (!string.IsNullOrEmpty(Text)) toggle.InnerHtml.Append(" " + Text);
        output.PreContent.AppendHtml(toggle);

        var menu = new TagBuilder("div");
        //grand-action-menu scopes the component style: the rich-text editor on the same page
        //draws its own .dropdown-menu, which must keep its legacy look
        menu.AddCssClass("dropdown-menu dropdown-menu-end grand-action-menu");
        menu.InnerHtml.AppendHtml(items);
        output.Content.SetHtmlContent(menu);
    }
}

/// <summary>
///     One entry of an &lt;admin-action-menu&gt;: a link, a submit button posting the surrounding
///     form to its own url, or a plain button a view's script drives by id.
/// </summary>
[HtmlTargetElement("admin-action", ParentTag = "admin-action-menu")]
public class AdminActionTagHelper : TagHelper
{
    [HtmlAttributeName("text")] public string Text { get; set; }

    /// <summary>A bootstrap-icons name ("bi-download").</summary>
    [HtmlAttributeName("icon")]
    public string Icon { get; set; }

    /// <summary>Where the entry goes: an href, or the form action when <see cref="Submit" />.</summary>
    [HtmlAttributeName("url")]
    public string Url { get; set; }

    /// <summary>Posts the surrounding form to <see cref="Url" /> instead of following it.</summary>
    [HtmlAttributeName("submit")]
    public bool Submit { get; set; }

    [HtmlAttributeName("name")] public string Name { get; set; }

    [HtmlAttributeName("id")] public string Id { get; set; }

    /// <summary>Draws the entry as destructive.</summary>
    [HtmlAttributeName("destructive")]
    public bool Destructive { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var isButton = Submit || string.IsNullOrEmpty(Url);
        output.TagName = isButton ? "button" : "a";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("dropdown-item", HtmlEncoder.Default);
        if (Destructive) output.AddClass("text-danger", HtmlEncoder.Default);
        if (!string.IsNullOrEmpty(Id)) output.Attributes.SetAttribute("id", Id);

        if (isButton)
        {
            output.Attributes.SetAttribute("type", Submit ? "submit" : "button");
            //a submit entry posts the form it sits in to its own url, so one form can carry
            //several exports without a helper per action
            if (Submit && !string.IsNullOrEmpty(Url)) output.Attributes.SetAttribute("formaction", Url);
            if (!string.IsNullOrEmpty(Name)) output.Attributes.SetAttribute("name", Name);
        }
        else
        {
            output.Attributes.SetAttribute("href", Url);
        }

        if (!string.IsNullOrEmpty(Icon)) output.PreContent.AppendHtml($"<i class=\"bi {Icon}\"></i> ");
        output.Content.Append(Text ?? string.Empty);
    }
}
