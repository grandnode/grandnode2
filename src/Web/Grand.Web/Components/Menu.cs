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
    private readonly IWorkContextAccessor _workContextAccessor;

    public MenuViewComponent(
        IMediator mediator,
        IWorkContextAccessor workContextAccessor,
        IPermissionService permissionService)
    {
        _mediator = mediator;
        _workContextAccessor = workContextAccessor;
        _permissionService = permissionService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await _permissionService.Authorize(StandardPermission.PublicStoreAllowNavigation,
                _workContextAccessor.WorkContext.CurrentCustomer))
            return Content("");

        var model = await _mediator.Send(new GetMenu {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });

        return View(model);
    }
}