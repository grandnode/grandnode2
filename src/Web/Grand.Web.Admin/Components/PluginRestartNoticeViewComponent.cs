using Grand.Domain.Permissions;
using Grand.Infrastructure.Plugins;
using Grand.Web.Common.Components;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components;

/// <summary>
///     Lists the plugin changes waiting for a restart. Computed on every request, not cached: the notice goes
///     away right after the restart and shows changes another administrator made.
/// </summary>
public class PluginRestartNoticeViewComponent : BaseAdminViewComponent
{
    private readonly IRequestPermissionService _permissionService;

    public PluginRestartNoticeViewComponent(IRequestPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await _permissionService.Authorize(StandardPermission.ManagePlugins))
            return Content("");

        var changes = await PluginManager.GetPendingChanges();
        if (changes.Count == 0)
            return Content("");

        return View(changes);
    }
}
