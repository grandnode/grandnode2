using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Security;
using Grand.Web.Admin.Models.Cms;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components;

/// <summary>
///     Renders a landing page (by system name) as a card on the admin dashboard - the Admin counterpart
///     of the StorePage and VendorPage components. Renders nothing when the page does not exist (an
///     installation that has not run the 2.4 upgrade yet) or has neither title nor body.
/// </summary>
public class AdminPageViewComponent : BaseAdminViewComponent
{
    private readonly IPageService _pageService;
    private readonly IPermissionService _permissionService;
    private readonly IHtmlSanitizationService _htmlSanitizationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly ILogger<AdminPageViewComponent> _logger;

    public AdminPageViewComponent(
        IPageService pageService,
        IPermissionService permissionService,
        IHtmlSanitizationService htmlSanitizationService,
        IContextAccessor contextAccessor,
        ILogger<AdminPageViewComponent> logger)
    {
        _pageService = pageService;
        _permissionService = permissionService;
        _htmlSanitizationService = htmlSanitizationService;
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync(string systemName)
    {
        var page = await _pageService.GetPageBySystemName(systemName, "");
        //a store manager can only create pages limited to their own store; such a page must not
        //end up on the dashboard of every administrator
        if (page == null || page.LimitedToStores)
            return Content("");

        var languageId = _contextAccessor.WorkContext.WorkingLanguage?.Id ?? "";
        var title = page.GetTranslation(x => x.Title, languageId);
        var body = page.GetTranslation(x => x.Body, languageId);
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
            return Content("");

        //the body is checked against the rich-text allowlist when it is saved; data written around the
        //admin form (import, API, database) is checked here too before it is rendered unencoded
        if (_htmlSanitizationService.ContainsDisallowedRichText(body))
        {
            _logger.LogWarning("The body of page {SystemName} contains markup outside the allowlist and was not rendered",
                systemName);
            body = null;
        }

        var canEdit = await _permissionService.Authorize(StandardPermission.ManagePages);
        return View(new AdminPortalModel(page.Id, title, body, canEdit));
    }
}
