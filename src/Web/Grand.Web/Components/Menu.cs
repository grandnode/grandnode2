using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class MenuViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;
    private readonly IContextAccessor _contextAccessor;

    public MenuViewComponent(
        IMediator mediator,
        IContextAccessor contextAccessor,
        IPermissionService permissionService)
    {
        _mediator = mediator;
        _contextAccessor = contextAccessor;
        _permissionService = permissionService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await _permissionService.Authorize(StandardPermission.PublicStoreAllowNavigation,
                _contextAccessor.WorkContext.CurrentCustomer))
            return Content("");

        var model = await _mediator.Send(new GetMenu {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore
        });

        return View(model);
    }
}