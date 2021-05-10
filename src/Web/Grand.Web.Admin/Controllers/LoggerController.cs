using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Services.Security;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Logging;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.SystemLog)]
    public partial class LoggerController : BaseAdminController
    {
        private readonly ILogViewModelService _logViewModelService;
        private readonly ILogger _logger;
        private readonly ITranslationService _translationService;

        public LoggerController(ILogViewModelService logViewModelService, ILogger logger,
            ITranslationService translationService)
        {
            _logViewModelService = logViewModelService;
            _logger = logger;
            _translationService = translationService;
        }

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List()
        {
            var model = _logViewModelService.PrepareLogListModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> LogList(DataSourceRequest command, LogListModel model)
        {
            var (logModels, totalCount) = await _logViewModelService.PrepareLogModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = logModels.ToList(),
                Total = totalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> ClearAll()
        {
            await _logger.ClearLog();

            Success(_translationService.GetResource("Admin.System.Log.Cleared"));
            return RedirectToAction("List");
        }

        public new async Task<IActionResult> View(string id)
        {
            var log = await _logger.GetLogById(id);
            if (log == null)
                //No log found with the specified id
                return RedirectToAction("List");

            var model = await _logViewModelService.PrepareLogModel(log);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var log = await _logger.GetLogById(id);
            if (log == null)
                //No log found with the specified id
                return RedirectToAction("List");
            if (ModelState.IsValid)
            {
                await _logger.DeleteLog(log);
                Success(_translationService.GetResource("Admin.System.Log.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("View", id);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelected(ICollection<string> selectedIds)
        {
            if (ModelState.IsValid)
            {
                if (selectedIds != null)
                {
                    var logItems = await _logger.GetLogByIds(selectedIds.ToArray());
                    foreach (var logItem in logItems)
                        await _logger.DeleteLog(logItem);
                }
                return Json(new { Result = true });
            }
            return ErrorForKendoGridJson(ModelState);
        }
    }
}
