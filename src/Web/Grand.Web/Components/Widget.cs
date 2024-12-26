﻿using Grand.Business.Core.Interfaces.Cms;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Web.Common.Components;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Cms;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class WidgetViewComponent : BaseViewComponent
{
    private readonly ICacheBase _cacheBase;
    private readonly IWidgetService _widgetService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public WidgetViewComponent(IWorkContextAccessor workContextAccessor, ICacheBase cacheBase, IWidgetService widgetService)
    {
        _workContextAccessor = workContextAccessor;
        _cacheBase = cacheBase;
        _widgetService = widgetService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData = null)
    {
        var cacheKey = string.Format(CacheKeyConst.WIDGET_MODEL_KEY, _workContextAccessor.WorkContext.CurrentStore.Id,
            string.Join(",", _workContextAccessor.WorkContext.CurrentCustomer.GetCustomerGroupIds()),
            widgetZone);

        var cachedModel = await _cacheBase.GetAsync(cacheKey, async () =>
        {
            //model
            var model = new List<WidgetModel>();
            var widgets = await _widgetService.LoadActiveWidgetsByWidgetZone(widgetZone, _workContextAccessor.WorkContext.CurrentStore.Id,
                _workContextAccessor.WorkContext.CurrentCustomer);
            foreach (var widget in widgets)
            {
                var viewComponentName = await widget.GetPublicViewComponentName(widgetZone);
                var widgetModel = new WidgetModel {
                    ViewComponentName = viewComponentName,
                    WidgetZone = widgetZone
                };

                model.Add(widgetModel);
            }

            return model;
        });

        if (!cachedModel.Any())
            return Content("");

        if (additionalData == null) return View(cachedModel);

        foreach (var item in cachedModel) item.AdditionalData = additionalData;

        return View(cachedModel);
    }
}