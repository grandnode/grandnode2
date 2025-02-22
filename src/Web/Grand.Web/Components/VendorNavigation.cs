using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class VendorNavigationViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly VendorSettings _vendorSettings;
    private readonly IContextAccessor _contextAccessor;

    public VendorNavigationViewComponent(
        IContextAccessor contextAccessor,
        IMediator mediator,
        VendorSettings vendorSettings)
    {
        _contextAccessor = contextAccessor;
        _mediator = mediator;
        _vendorSettings = vendorSettings;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (_vendorSettings.VendorsBlockItemsToDisplay == 0)
            return Content("");

        var model = await _mediator.Send(new GetVendorNavigation {
            Language = _contextAccessor.WorkContext.WorkingLanguage
        });

        return !model.Vendors.Any() ? Content("") : View(model);
    }
}