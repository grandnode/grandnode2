using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;
using System.Text.Json;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     The "pick a file and import it" window every list screen opens from its toolbar.
///     Eight views used to spell this out by hand, each with its own column widths, button
///     class and inline styles; this renders the one shape from
///     <c>Partials/ImportWindow</c> and wires the toolbar button to it, the way
///     <see cref="AdminDeleteConfirmationTagHelper" /> does for a delete confirmation.
///     Whatever the view writes between the tags - the tip, the notes - is shown above the
///     field.
/// </summary>
[HtmlTargetElement("admin-import-window")]
public class AdminImportWindowTagHelper : TagHelper
{
    private readonly IContextAccessor _contextAccessor;
    private readonly IHtmlHelper _htmlHelper;
    private readonly ITranslationService _translationService;

    public AdminImportWindowTagHelper(IHtmlHelper htmlHelper, ITranslationService translationService,
        IContextAccessor contextAccessor)
    {
        _htmlHelper = htmlHelper;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

    [ViewContext] public ViewContext ViewContext { get; set; }

    /// <summary>Id of the window element; defaults to "{file-name}-window".</summary>
    [HtmlAttributeName("id")]
    public string WindowId { get; set; }

    /// <summary>Id of the toolbar button that opens it.</summary>
    [HtmlAttributeName("button-id")]
    public string ButtonId { get; set; }

    /// <summary>Title of the window.</summary>
    [HtmlAttributeName("title")]
    public string Title { get; set; }

    /// <summary>Controller the form posts to; defaults to the current one.</summary>
    [HtmlAttributeName("controller")]
    public string Controller { get; set; }

    [HtmlAttributeName("action")] public string Action { get; set; }

    /// <summary>Route id of the edited record, for an import that belongs to one.</summary>
    [HtmlAttributeName("route-id")]
    public string RouteId { get; set; }

    /// <summary>Posted name of the file field - part of the server contract.</summary>
    [HtmlAttributeName("file-name")]
    public string FileName { get; set; }

    /// <summary>Element id of the file field; defaults to the posted name.</summary>
    [HtmlAttributeName("file-id")]
    public string FileId { get; set; }

    [HtmlAttributeName("file-label")] public string FileLabel { get; set; }

    /// <summary>
    ///     Resource key of the label, for a resource an existing installation may not have
    ///     imported yet. When it resolves to nothing, <see cref="FileLabel" /> is shown
    ///     instead of the raw key - the same fallback
    ///     <see cref="AdminCultureTagHelper" /> makes for the widget texts.
    /// </summary>
    [HtmlAttributeName("file-label-key")]
    public string FileLabelKey { get; set; }

    [HtmlAttributeName("accept")] public string Accept { get; set; }

    [HtmlAttributeName("submit-text")] public string SubmitText { get; set; }

    [HtmlAttributeName("submit-id")] public string SubmitId { get; set; }

    private string ResolveFileLabel()
    {
        if (string.IsNullOrEmpty(FileLabelKey)) return FileLabel;
        var languageId = _contextAccessor?.WorkContext?.WorkingLanguage?.Id;
        var resource = string.IsNullOrEmpty(languageId)
            ? null
            : _translationService.GetResource(FileLabelKey, languageId, string.Empty, true);
        return string.IsNullOrEmpty(resource) ? FileLabel : resource;
    }

    public override async Task ProcessAsync(TagHelperContext tagHelperContext, TagHelperOutput output)
    {
        var windowId = string.IsNullOrEmpty(WindowId) ? $"{FileName}-window" : WindowId;

        var model = new ImportWindowModel {
            WindowId = windowId,
            AreaName = ViewContext.RouteData.Values["area"]?.ToString(),
            ControllerName = string.IsNullOrEmpty(Controller)
                ? ViewContext.RouteData.Values["controller"]?.ToString()
                : Controller,
            ActionName = Action,
            RouteId = RouteId,
            FileName = FileName,
            FileId = string.IsNullOrEmpty(FileId) ? FileName : FileId,
            FileLabel = ResolveFileLabel(),
            Accept = Accept,
            SubmitText = SubmitText,
            SubmitId = SubmitId,
            Notes = await output.GetChildContentAsync()
        };

        (_htmlHelper as IViewContextAware)?.Contextualize(ViewContext);

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("id", windowId);
        output.Attributes.SetAttribute("style", "display:none");

        output.Content.SetHtmlContent((await _htmlHelper.PartialAsync("Partials/ImportWindow", model))
            .ToHtmlString());

        //GrandAdmin.modal (adminapp/src/ui/modal.js); the title is JSON-encoded, so an
        //apostrophe in the translation cannot end the string literal it is pasted into
        if (string.IsNullOrEmpty(ButtonId)) return;

        var script = new StringBuilder();
        script.AppendLine("<script>");
        script.AppendLine("$(document).ready(function() {");
        script.AppendLine($"document.getElementById({JsonSerializer.Serialize(ButtonId)})?.addEventListener('click', function (e) {{");
        script.AppendLine("e.preventDefault();");
        script.AppendLine(
            $"GrandAdmin.modal.open({JsonSerializer.Serialize(windowId)}, {{ title: {JsonSerializer.Serialize(Title ?? string.Empty)}, actions: ['Close'] }});");
        script.AppendLine("});");
        script.AppendLine("});");
        script.AppendLine("</script>");
        output.PostContent.SetHtmlContent(script.ToString());
    }
}
