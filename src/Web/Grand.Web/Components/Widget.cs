using Grand.Business.Cms.Interfaces;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Web.Common.Components;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Cms;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.ViewComponents
{
    public class WidgetViewComponent : BaseViewComponent
    {
        private readonly IWorkContext _workContext;
        private readonly ICacheBase _cacheBase;
        private readonly IWidgetService _widgetService;

        public WidgetViewComponent(IWorkContext workContext, ICacheBase cacheBase, IWidgetService widgetService)
        {
            _workContext = workContext;
            _cacheBase = cacheBase;
            _widgetService = widgetService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData = null)
        {
            var cacheKey = string.Format(CacheKeyConst.WIDGET_MODEL_KEY, _workContext.CurrentStore.Id,
                string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
                widgetZone);

            var cachedModel = await _cacheBase.GetAsync(cacheKey, async () =>
            {
                //model
                var model = new List<WidgetModel>();
                var widgets = await _widgetService.LoadActiveWidgetsByWidgetZone(widgetZone, _workContext.CurrentStore.Id, _workContext.CurrentCustomer);
                foreach (var widget in widgets)
                {
                    var viewComponentName = await widget.GetPublicViewComponentName(widgetZone);
                    var widgetModel = new WidgetModel {
                        ViewComponentName = viewComponentName,
                        WidgetZone = widgetZone
                    };

                    model.Add(widgetModel);
                }
                return await Task.FromResult(model);
            });

            if (!cachedModel.Any())
                return Content("");

            if (additionalData != null)
                foreach (var item in cachedModel)
                {
                    item.AdditionalData = additionalData;
                }

            return View(cachedModel);
        }
    }
}