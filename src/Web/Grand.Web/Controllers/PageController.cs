using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Infrastructure;
using Grand.Web.Features.Models.Pages;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Grand.Web.Controllers
{
    public partial class PageController : BasePublicController
    {
        #region Fields

        private readonly ITranslationService _translationService;
        private readonly IPermissionService _permissionService;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;

        #endregion

        #region Constructors

        public PageController(
            ITranslationService translationService,
            IPermissionService permissionService,
            IWorkContext workContext,
            IMediator mediator)
        {
            _translationService = translationService;
            _permissionService = permissionService;
            _workContext = workContext;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        public virtual async Task<IActionResult> PageDetails(string pageId)
        {
            if (string.IsNullOrEmpty(pageId))
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetPageBlock() { PageId = pageId });
            if (model == null)
                return RedirectToRoute("HomePage");

            //hide page if it`s set as no published
            if (!model.Published
                && !(await _permissionService.Authorize(StandardPermission.AccessAdminPanel))
                && !(await _permissionService.Authorize(StandardPermission.ManagePages)))
                return RedirectToRoute("HomePage");

            //layout
            var layoutViewPath = await _mediator.Send(new GetPageLayoutViewPath() { LayoutId = model.PageLayoutId });

            //display "edit" (manage) link
            if (await _permissionService.Authorize(StandardPermission.AccessAdminPanel) && await _permissionService.Authorize(StandardPermission.ManagePages))
                DisplayEditLink(Url.Action("Edit", "Page", new { id = model.Id, area = "Admin" }));

            return View(layoutViewPath, model);
        }

        public virtual async Task<IActionResult> PageDetailsPopup(string systemName)
        {
            var model = await _mediator.Send(new GetPageBlock() { SystemName = systemName });
            if (model == null)
                return RedirectToRoute("HomePage");

            //template
            var layoutViewPath = await _mediator.Send(new GetPageLayoutViewPath() { LayoutId = model.PageLayoutId });

            ViewBag.IsPopup = true;
            return View(layoutViewPath, model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Authenticate(string id, string password)
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { Authenticated = false, Error = "Empty id" });

            var authResult = false;
            var title = string.Empty;
            var body = string.Empty;
            var error = string.Empty;

            var page = await _mediator.Send(new GetPageBlock() { PageId = id, Password = password });

            if (page != null &&
                //password protected?
                page.IsPasswordProtected)
            {
                if (page.Password != null && page.Password.Equals(password))
                {
                    authResult = true;
                    title = page.Title;
                    body = page.Body;
                }
                else
                {
                    error = _translationService.GetResource("Page.WrongPassword");
                }
            }
            return Json(new { Authenticated = authResult, Title = title, Body = body, Error = error });
        }

        #endregion
    }
}
