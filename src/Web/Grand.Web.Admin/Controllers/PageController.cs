using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Pages;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Pages)]
    public partial class PageController : BaseAdminController
    {
        #region Fields

        private readonly IPageViewModelService _pageViewModelService;
        private readonly IPageService _pageService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IDateTimeService _dateTimeService;

        #endregion Fields

        #region Constructors

        public PageController(
            IPageViewModelService pageViewModelService,
            IPageService pageService,
            ILanguageService languageService,
            ITranslationService translationService,
            IWorkContext workContext,
            IDateTimeService dateTimeService)
        {
            _pageViewModelService = pageViewModelService;
            _pageService = pageService;
            _languageService = languageService;
            _translationService = translationService;
            _workContext = workContext;
            _dateTimeService = dateTimeService;
        }

        #endregion

        #region List

        public IActionResult Index() => RedirectToAction("List");

        public async Task<IActionResult> List()
        {
            var model = await _pageViewModelService.PreparePageListModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command, PageListModel model)
        {
            var pageModels = (await _pageService.GetAllPages(model.SearchStoreId, true))
                .Select(x => x.ToModel(_dateTimeService))
                .ToList();

            if (!string.IsNullOrEmpty(model.Name))
            {
                pageModels = pageModels.Where
                    (x => x.SystemName.ToLowerInvariant().Contains(model.Name.ToLowerInvariant()) ||
                    (x.Title != null && x.Title.ToLowerInvariant().Contains(model.Name.ToLowerInvariant()))).ToList();
            }
            //"Error during serialization or deserialization using the JSON JavaScriptSerializer. The length of the string exceeds the value set on the maxJsonLength property. "
            foreach (var page in pageModels)
            {
                page.Body = "";
            }
            var gridModel = new DataSourceResult
            {
                Data = pageModels,
                Total = pageModels.Count
            };

            return Json(gridModel);
        }

        #endregion

        #region Create / Edit / Delete

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new PageModel();
            //layouts
            await _pageViewModelService.PrepareLayoutsModel(model);
            //locales
            await AddLocales(_languageService, model.Locales);
            //default values
            model.DisplayOrder = 1;

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(PageModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var page = await _pageViewModelService.InsertPageModel(model);
                Success(_translationService.GetResource("Admin.Content.Pages.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = page.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            //layouts
            await _pageViewModelService.PrepareLayoutsModel(model);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var page = await _pageService.GetPageById(id);
            if (page == null)
                //No page found with the specified id
                return RedirectToAction("List");

            var model = page.ToModel(_dateTimeService);
            model.Url = Url.RouteUrl("Page", new { SeName = page.GetSeName(_workContext.WorkingLanguage.Id) }, "http");
            //layouts
            await _pageViewModelService.PrepareLayoutsModel(model);
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Title = page.GetTranslation(x => x.Title, languageId, false);
                locale.Body = page.GetTranslation(x => x.Body, languageId, false);
                locale.MetaKeywords = page.GetTranslation(x => x.MetaKeywords, languageId, false);
                locale.MetaDescription = page.GetTranslation(x => x.MetaDescription, languageId, false);
                locale.MetaTitle = page.GetTranslation(x => x.MetaTitle, languageId, false);
                locale.SeName = page.GetSeName(languageId, false);
            });
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(PageModel model, bool continueEditing)
        {
            if (model.StartDateUtc.HasValue && model.EndDateUtc.HasValue && model.StartDateUtc >= model.EndDateUtc)
            {
                ModelState.AddModelError(nameof(model.StartDateUtc), "Start Date cannot be later than End Date");
            }
            var page = await _pageService.GetPageById(model.Id);
            if (page == null)
                //No page found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                page = await _pageViewModelService.UpdatePageModel(page, model);
                Success(_translationService.GetResource("Admin.Content.Pages.Updated"));
                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = page.Id });
                }
                return RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            model.Url = Url.RouteUrl("Page", new { SeName = page.GetSeName(_workContext.WorkingLanguage.Id) }, "http");
            //layouts
            await _pageViewModelService.PrepareLayoutsModel(model);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var page = await _pageService.GetPageById(id);
            if (page == null)
                //No page found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _pageViewModelService.DeletePage(page);
                Success(_translationService.GetResource("Admin.Content.Pages.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = id });
        }

        #endregion
    }
}
