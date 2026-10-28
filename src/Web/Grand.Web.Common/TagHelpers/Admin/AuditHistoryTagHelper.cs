using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     The "Change history" button that opens the audit history of one record (entity-id set) or of one entity type.
///     Renders nothing outside Main Admin or without the audit log permission, so a shared view stays safe in
///     the Store and Vendor panels.
/// </summary>
[HtmlTargetElement("audit-history", TagStructure = TagStructure.WithoutEndTag)]
public class AuditHistoryTagHelper(
    IRequestPermissionService permissionService,
    ITranslationService translationService,
    IContextAccessor contextAccessor) : TagHelper
{
    private const string AdminArea = "Admin";

    [ViewContext] [HtmlAttributeNotBound] public ViewContext ViewContext { get; set; }

    [HtmlAttributeName("entity-type")] public string EntityType { get; set; }

    [HtmlAttributeName("entity-id")] public string EntityId { get; set; }

    /// <summary>
    ///     Comma-separated entity names to limit a per-type history to, e.g. the setting classes of a settings page
    /// </summary>
    [HtmlAttributeName("entity-names")] public string EntityNames { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        if (ViewContext.RouteData.Values["area"] as string != AdminArea || string.IsNullOrEmpty(EntityType)
            || !await permissionService.Authorize(PermissionSystemName.AuditLog))
        {
            output.SuppressOutput();
            return;
        }

        var url = $"{ViewContext.HttpContext.Request.PathBase}/{AdminArea}/AuditLog/HistoryPopup" +
                  $"?entityType={Uri.EscapeDataString(EntityType)}" +
                  (string.IsNullOrEmpty(EntityId) ? "" : $"&entityId={Uri.EscapeDataString(EntityId)}") +
                  (string.IsNullOrEmpty(EntityNames) ? "" : $"&entityNames={Uri.EscapeDataString(EntityNames)}");
        var id = "audit-history-" + Guid.NewGuid().ToString("N")[..8];
        var text = AdminText.Resource(translationService, contextAccessor, "Admin.System.AuditLog.ChangeHistory",
            "Change history");
        var encoder = HtmlEncoder.Default;

        output.Content.SetHtmlContent(
            $"<a id=\"{id}\" href=\"{encoder.Encode(url)}\" class=\"btn btn-outline-secondary btn-sm\">" +
            $"<i class=\"bi bi-clock-history\"></i> {encoder.Encode(text)}</a>" +
            "<script>$(function () { $('#" + id + "').magnificPopup({ type: 'ajax', " +
            "settings: { cache: false }, midClick: true, callbacks: { " +
            "parseAjax: function (r) { r.data = $('<div></div>').html(r.data); }, " +
            "ajaxContentAdded: function () { $('.mfp-wrap').removeAttr('tabindex'); } } }); });</script>");
    }
}
