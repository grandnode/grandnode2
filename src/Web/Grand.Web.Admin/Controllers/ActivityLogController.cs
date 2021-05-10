using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.ActivityLog)]
    public partial class ActivityLogController : BaseAdminController
    {
        #region Fields

        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IActivityLogViewModelService _activityLogViewModelService;

        #endregion Fields

        #region Constructors

        public ActivityLogController(ICustomerActivityService customerActivityService,
            ITranslationService translationService, IActivityLogViewModelService activityLogViewModelService)
        {
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _activityLogViewModelService = activityLogViewModelService;
        }

        #endregion

        #region Activity log types
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> ListTypes()
        {
            var model = await _activityLogViewModelService.PrepareActivityLogTypeModels();
            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> SaveTypes(IFormCollection form)
        {
            string formKey = "checkbox_activity_types";
            var checkedActivityTypes = form[formKey].ToString() != null ? form[formKey].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x).ToList() : new List<string>();

            await _activityLogViewModelService.SaveTypes(checkedActivityTypes);

            Success(_translationService.GetResource("Admin.Settings.ActivityLog.ActivityLogType.Updated"));
            return RedirectToAction("ListTypes");
        }

        #endregion

        #region Activity log
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> ListLogs()
        {
            var model = await _activityLogViewModelService.PrepareActivityLogSearchModel();
            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> ListLogs(DataSourceRequest command, ActivityLogSearchModel model)
        {
            var activitymodel = await _activityLogViewModelService.PrepareActivityLogModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = activitymodel.activityLogs,
                Total = activitymodel.totalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> ActivityLogDelete(string id)
        {
            var activityLog = await _customerActivityService.GetActivityById(id);
            if (activityLog == null)
            {
                throw new ArgumentException("No activity log found with the specified id");
            }
            if (ModelState.IsValid)
            {
                await _customerActivityService.DeleteActivity(activityLog);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> ClearAll()
        {
            await _customerActivityService.ClearAllActivities();
            return RedirectToAction("ListLogs");
        }

        #endregion

        #region Activity Stats
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> ListStats()
        {
            var model = await _activityLogViewModelService.PrepareActivityLogSearchModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> ListStats(DataSourceRequest command, ActivityLogSearchModel model)
        {
            var activityStatmodel = await _activityLogViewModelService.PrepareActivityStatModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = activityStatmodel.activityStats,
                Total = activityStatmodel.totalCount
            };
            return Json(gridModel);

        }
        #endregion

    }
}
