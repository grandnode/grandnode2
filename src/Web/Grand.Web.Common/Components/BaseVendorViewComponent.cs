using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Common.Components;

[Area("Vendor")]
public abstract class BaseVendorViewComponent : ViewComponent
{
    public new IViewComponentResult View<TModel>(string viewName, TModel model)
    {
        return base.View(viewName, model);
    }

    public new IViewComponentResult View<TModel>(TModel model)
    {
        return base.View(model);
    }

    public new IViewComponentResult View(string viewName)
    {
        return base.View(viewName);
    }
}