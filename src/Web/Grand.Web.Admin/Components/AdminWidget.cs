using Grand.Business.Core.Interfaces.Cms;
using Grand.Infrastructure;
using Grand.Web.Admin.Models.Cms;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components;

public class AdminWidgetViewComponent : BaseAdminViewComponent
{
    #region Constructors

    public AdminWidgetViewComponent(IWidgetService widgetService, IWorkContextAccessor workContextAccessor)
    {
        _widgetService = widgetService;
        _workContextAccessor = workContextAccessor;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData = null)
    {
        var model = new List<AdminWidgetModel>();

        var widgets = await _widgetService.LoadActiveWidgetsByWidgetZone(widgetZone, _workContextAccessor.WorkContext.CurrentStore.Id,
            _workContextAccessor.WorkContext.CurrentCustomer);
        foreach (var item in widgets)
        {
            var viewComponentName = await item.GetPublicViewComponentName(widgetZone);
            var widgetModel = new AdminWidgetModel {
                WidgetZone = widgetZone,
                ViewComponentName = viewComponentName
            };
            model.Add(widgetModel);
        }

        if (!model.Any())
            return Content("");

        if (additionalData != null)
            foreach (var item in model)
                item.AdditionalData = additionalData;
        return View(model);
    }

    #endregion

    #region Fields

    private readonly IWidgetService _widgetService;
    private readonly IWorkContextAccessor _workContextAccessor;

    #endregion
}