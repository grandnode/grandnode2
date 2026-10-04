using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.SharedKernel.Attributes;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Features.Models.Pages;
using Grand.Web.Models.Pages;
using Grand.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers;

[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class PageController : BasePublicController
{
    #region Constructors

    public PageController(
        ITranslationService translationService,
        IPermissionService permissionService,
        IMediator mediator)
    {
        _translationService = translationService;
        _permissionService = permissionService;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly ITranslationService _translationService;
    private readonly IPermissionService _permissionService;
    private readonly IMediator _mediator;

    #endregion

    #region Methods

    [HttpGet]
    public virtual async Task<IActionResult> PageDetails(string pageId)
    {
        if (string.IsNullOrEmpty(pageId))
            return RedirectToRoute("HomePage");

        //Check whether the current user has a "Manage pages" permission
        //It allows him to preview a page before publishing or outside its date range
        var canPreview = await _permissionService.Authorize(StandardPermission.ManageAccessAdminPanel) &&
                         await _permissionService.Authorize(StandardPermission.ManagePages);

        var model = await _mediator.Send(new GetPageBlock { PageId = pageId, ShowHidden = canPreview });
        if (model == null)
            return RedirectToRoute("HomePage");

        //layout
        var layoutViewPath = await _mediator.Send(new GetPageLayoutViewPath { LayoutId = model.PageLayoutId });

        //display "edit" (manage) link
        if (await _permissionService.Authorize(StandardPermission.ManageAccessAdminPanel) &&
            await _permissionService.Authorize(StandardPermission.ManagePages))
            DisplayEditLink(Url.Action("Edit", "Page", new { id = model.Id, area = "Admin" }));

        return View(layoutViewPath, model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> PageDetailsPopup(string systemName)
    {
        var model = await _mediator.Send(new GetPageBlock { SystemName = systemName });
        if (model == null)
            return RedirectToRoute("HomePage");

        //template
        var layoutViewPath = await _mediator.Send(new GetPageLayoutViewPath { LayoutId = model.PageLayoutId });

        ViewBag.IsPopup = true;
        return View(layoutViewPath, model);
    }

    [DenySystemAccount]
    [HttpPost]
    public virtual async Task<IActionResult> Authenticate(AuthenticateModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { Authenticated = false, Error = "Model is not valid" });

        if (string.IsNullOrEmpty(model.Id))
            return Json(new { Authenticated = false, Error = "Empty id" });

        var authResult = false;
        var title = string.Empty;
        var body = string.Empty;
        var error = string.Empty;

        var page = await _mediator.Send(new GetPageBlock { PageId = model.Id, Password = model.Password });

        if (page is not { IsPasswordProtected: true })
            return Json(new { Authenticated = false, Title = title, Body = body, Error = error });
        if (page.Password != null && page.Password.Equals(model.Password))
        {
            authResult = true;
            title = page.Title;
            body = page.Body;
        }
        else
        {
            error = _translationService.GetResource("Page.WrongPassword");
        }

        return Json(new { Authenticated = authResult, Title = title, Body = body, Error = error });
    }

    #endregion
}