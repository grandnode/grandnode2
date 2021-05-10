using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Grand.Web.ViewComponents
{
    public class MenuViewComponent : BaseViewComponent
    {
        private readonly IMediator _mediator;
        private readonly IWorkContext _workContext;
        private readonly IPermissionService _permissionService;

        public MenuViewComponent(
            IMediator mediator,
            IWorkContext workContext,
            IPermissionService permissionService)
        {
            _mediator = mediator;
            _workContext = workContext;
            _permissionService = permissionService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!await _permissionService.Authorize(StandardPermission.PublicStoreAllowNavigation, _workContext.CurrentCustomer))
                return Content("");

            var model = await _mediator.Send(new GetMenu()
            {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });

            return View(model);
        }
    }
}