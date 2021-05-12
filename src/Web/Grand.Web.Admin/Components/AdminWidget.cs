using Grand.Business.Cms.Interfaces;
using Grand.Infrastructure;
using Grand.Web.Admin.Models.Cms;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Components
{
    public class AdminWidgetViewComponent : BaseAdminViewComponent
    {
        #region Fields

        private readonly IWidgetService _widgetService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Constructors

        public AdminWidgetViewComponent(IWidgetService widgetService, IWorkContext workContext)
        {
            _widgetService = widgetService;
            _workContext = workContext;
        }

        #endregion

        #region Invoker

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData = null)
        {
            var model = new List<AdminWidgetModel>();

            var widgets = await _widgetService.LoadActiveWidgetsByWidgetZone(widgetZone, _workContext.CurrentStore.Id, _workContext.CurrentCustomer);
            foreach (var item in widgets)
            {
                var viewComponentName = await item.GetPublicViewComponentName(widgetZone);
                var widgetModel = new AdminWidgetModel {
                    WidgetZone = widgetZone,
                    ViewComponentName = viewComponentName,
                };
                model.Add(widgetModel);
            }

            if (!model.Any())
                return Content("");

            if (additionalData != null)
                foreach (var item in model)
                {
                    item.AdditionalData = additionalData;
                }
            return View(model);
        }

        #endregion
    }
}