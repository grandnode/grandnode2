using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class GetCoordinateViewComponent : BaseViewComponent
{
    private readonly CustomerSettings _customerSettings;
    private readonly IWorkContextAccessor _workContextAccessor;

    public GetCoordinateViewComponent(CustomerSettings customerSettings, IWorkContextAccessor workContextAccessor)
    {
        _customerSettings = customerSettings;
        _workContextAccessor = workContextAccessor;
    }

    public IViewComponentResult Invoke()
    {
        if (!_customerSettings.GeoEnabled)
            return Content("");

        if (_workContextAccessor.WorkContext.CurrentCustomer.Coordinates == null)
            return View(new LocationModel());

        var model = new LocationModel {
            Longitude = _workContextAccessor.WorkContext.CurrentCustomer.Coordinates.X,
            Latitude = _workContextAccessor.WorkContext.CurrentCustomer.Coordinates.Y
        };
        return View(model);
    }
}