using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class GetCoordinateViewComponent : BaseViewComponent
{
    private readonly CustomerSettings _customerSettings;
    private readonly IContextAccessor _contextAccessor;

    public GetCoordinateViewComponent(CustomerSettings customerSettings, IContextAccessor contextAccessor)
    {
        _customerSettings = customerSettings;
        _contextAccessor = contextAccessor;
    }

    public IViewComponentResult Invoke()
    {
        if (!_customerSettings.GeoEnabled)
            return Content("");

        if (_contextAccessor.WorkContext.CurrentCustomer.Coordinates == null)
            return View(new LocationModel());

        var model = new LocationModel {
            Longitude = _contextAccessor.WorkContext.CurrentCustomer.Coordinates.X,
            Latitude = _contextAccessor.WorkContext.CurrentCustomer.Coordinates.Y
        };
        return View(model);
    }
}