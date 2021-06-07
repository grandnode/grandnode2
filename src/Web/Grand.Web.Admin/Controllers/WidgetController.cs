using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Cms;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Plugins;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Cms;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Widgets)]
    public partial class WidgetController : BaseAdminController
    {
        #region Fields

        private readonly IWidgetService _widgetService;
        private readonly ISettingService _settingService;
        private readonly ICacheBase _cacheBase;
        private readonly IServiceProvider _serviceProvider;
        private readonly WidgetSettings _widgetSettings;
        
        #endregion

        #region Constructors

        public WidgetController(IWidgetService widgetService,
            ISettingService settingService,
            ICacheBase cacheBase,
            IServiceProvider serviceProvider,
            WidgetSettings widgetSettings)
        {
            _widgetService = widgetService;
            _widgetSettings = widgetSettings;
            _cacheBase = cacheBase;
            _serviceProvider = serviceProvider;
            _settingService = settingService;
        }

        #endregion

        #region Methods

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View();

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public IActionResult List(DataSourceRequest command)
        {
            var widgetsModel = new List<WidgetModel>();
            var widgets = _widgetService.LoadAllWidgets();
            foreach (var widget in widgets)
            {
                var tmp = widget.ToModel();
                tmp.IsActive = widget.IsWidgetActive(_widgetSettings);

                var url = widget.ConfigurationUrl;
                if (string.IsNullOrEmpty(url))
                    url = PluginManager.ReferencedPlugins.FirstOrDefault(x => x.SystemName.Equals(widget.SystemName, StringComparison.OrdinalIgnoreCase))
                        ?.Instance<IPlugin>(_serviceProvider)?.ConfigurationUrl();
                tmp.ConfigurationUrl = url;

                widgetsModel.Add(tmp);
            }
            widgetsModel = widgetsModel.ToList();
            var gridModel = new DataSourceResult
            {
                Data = widgetsModel,
                Total = widgetsModel.Count()
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> WidgetUpdate(WidgetModel model)
        {
            var widget = _widgetService.LoadWidgetBySystemName(model.SystemName);
            if (widget.IsWidgetActive(_widgetSettings))
            {
                if (!model.IsActive)
                {
                    //remove from active
                    _widgetSettings.ActiveWidgetSystemNames.Remove(widget.SystemName);
                    await _settingService.SaveSetting(_widgetSettings);
                }
            }
            else
            {
                if (model.IsActive)
                {
                    //add to active list
                    _widgetSettings.ActiveWidgetSystemNames.Add(widget.SystemName);
                    await _settingService.SaveSetting(_widgetSettings);
                }
            }
            await _cacheBase.Clear();

            return new JsonResult("");
        }

        #endregion
    }
}
