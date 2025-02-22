using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Customers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class CustomerNavigationViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly IContextAccessor _contextAccessor;

    public CustomerNavigationViewComponent(IMediator mediator,
        IContextAccessor contextAccessor)
    {
        _mediator = mediator;
        _contextAccessor = contextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync(int selectedTabId = 0)
    {
        var model = await _mediator.Send(new GetNavigation {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            SelectedTabId = selectedTabId,
            Store = _contextAccessor.StoreContext.CurrentStore,
            Vendor = _contextAccessor.WorkContext.CurrentVendor
        });
        return View(model);
    }
}