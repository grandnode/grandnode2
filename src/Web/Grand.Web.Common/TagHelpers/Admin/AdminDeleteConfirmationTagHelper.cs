using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Json;

namespace Grand.Web.Common.TagHelpers.Admin;

[HtmlTargetElement("admin-delete-confirmation")]
public class AdminDeleteConfirmationTagHelper : TagHelper
{
    private readonly IHtmlHelper _htmlHelper;

    private readonly ITranslationService _translationService;

    public AdminDeleteConfirmationTagHelper(IHtmlHelper htmlHelper, ITranslationService translationService)
    {
        _htmlHelper = htmlHelper;
        _translationService = translationService;
    }

    [ViewContext] public ViewContext ViewContext { get; set; }

    [HtmlAttributeName("action-name")] public string Action { get; set; }

    [HtmlAttributeName("button-id")] public string ButtonId { get; set; }

    [HtmlAttributeName("id")] public string ModelId { get; set; }

    public override async Task ProcessAsync(TagHelperContext tagHelperContext, TagHelperOutput output)
    {
        if (string.IsNullOrEmpty(Action))
            Action = "Delete";

        var windowId =
            new HtmlString(ViewContext.ViewData.ModelMetadata.ModelType.Name.ToLower() + "-delete-confirmation")
                .ToHtmlString();

        var modelId = string.IsNullOrEmpty(ModelId) ? ViewContext.RouteData.Values["Id"]?.ToString() : ModelId;

        var deleteConfirmationModel = new DeleteConfirmationModel {
            Id = modelId,
            ControllerName = ViewContext.RouteData.Values["controller"]?.ToString(),
            ActionName = Action,
            WindowId = windowId
        };

        (_htmlHelper as IViewContextAware)?.Contextualize(ViewContext);

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("id", windowId);
        output.Attributes.SetAttribute("style", "display:none");

        output.Content.SetHtmlContent((await _htmlHelper.PartialAsync("Partials/Delete", deleteConfirmationModel))
            .ToHtmlString());

        //GrandAdmin.modal (adminapp/src/ui/modal.js) in place of the Kendo Window; the
        //title is JSON-encoded, so an apostrophe in the translation can no longer end the
        //string literal it used to be pasted into
        var title = JsonSerializer.Serialize(_translationService.GetResource("Admin.Common.AreYouSure"));
        var script = new StringBuilder();
        script.AppendLine("<script>");
        script.AppendLine("$(document).ready(function() {");
        script.AppendLine($"document.getElementById({JsonSerializer.Serialize(ButtonId)})?.addEventListener('click', function (e) {{");
        script.AppendLine("e.preventDefault();");
        script.AppendLine(
            $"GrandAdmin.modal.open({JsonSerializer.Serialize(windowId)}, {{ title: {title}, actions: ['Close'] }});");
        script.AppendLine("});");
        script.AppendLine("});");
        script.AppendLine("</script>");
        output.PostContent.SetHtmlContent(script.ToString());
    }
}