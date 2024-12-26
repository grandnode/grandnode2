using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Customers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class CustomerNavigationViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly IWorkContextAccessor _workContextAccessor;

    public CustomerNavigationViewComponent(IMediator mediator,
        IWorkContextAccessor workContextAccessor)
    {
        _mediator = mediator;
        _workContextAccessor = workContextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync(int selectedTabId = 0)
    {
        var model = await _mediator.Send(new GetNavigation {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            SelectedTabId = selectedTabId,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Vendor = _workContextAccessor.WorkContext.CurrentVendor
        });
        return View(model);
    }
}