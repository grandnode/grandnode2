﻿using Grand.Business.Core.Interfaces.Cms;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Vendor.Models.Cms;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Components;

public class VendorWidgetViewComponent : BaseVendorViewComponent
{
    #region Constructors

    public VendorWidgetViewComponent(IWidgetService widgetService, IWorkContextAccessor workContextAccessor)
    {
        _widgetService = widgetService;
        _workContextAccessor = workContextAccessor;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData = null)
    {
        var model = new List<VendorWidgetModel>();

        var widgets = await _widgetService.LoadActiveWidgetsByWidgetZone(widgetZone, _workContextAccessor.WorkContext.CurrentStore.Id,
            _workContextAccessor.WorkContext.CurrentCustomer);
        foreach (var item in widgets)
        {
            var viewComponentName = await item.GetPublicViewComponentName(widgetZone);
            var widgetModel = new VendorWidgetModel {
                WidgetZone = widgetZone,
                ViewComponentName = viewComponentName
            };
            model.Add(widgetModel);
        }

        if (!model.Any())
            return Content("");

        if (additionalData == null) return View(model);

        foreach (var item in model) item.AdditionalData = additionalData;

        return View(model);
    }

    #endregion

    #region Fields

    private readonly IWidgetService _widgetService;
    private readonly IWorkContextAccessor _workContextAccessor;

    #endregion
}