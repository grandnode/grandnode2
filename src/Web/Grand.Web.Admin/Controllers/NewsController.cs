using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.News;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.News;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.News)]
    public partial class NewsController : BaseAdminController
    {
        #region Fields
        private readonly INewsViewModelService _newsViewModelService;
        private readonly INewsService _newsService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly IStoreService _storeService;
        private readonly IGroupService _groupService;
        private readonly IDateTimeService _dateTimeService;
        #endregion

        #region Constructors

        public NewsController(
            INewsViewModelService newsViewModelService,
            INewsService newsService,
            ILanguageService languageService,
            ITranslationService translationService,
            IStoreService storeService,
            IGroupService groupService,
            IDateTimeService dateTimeService)
        {
            _newsViewModelService = newsViewModelService;
            _newsService = newsService;
            _languageService = languageService;
            _translationService = translationService;
            _storeService = storeService;
            _groupService = groupService;
            _dateTimeService = dateTimeService;
        }

        #endregion

        #region News items

        public IActionResult Index() => RedirectToAction("List");

        public async Task<IActionResult> List()
        {
            var model = new NewsItemListModel();
            //stores
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
            foreach (var s in await _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command, NewsItemListModel model)
        {
            var news = await _newsViewModelService.PrepareNewsItemModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = news.newsItemModels.ToList(),
                Total = news.totalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            ViewBag.AllLanguages = _languageService.GetAllLanguages(true);
            var model = new NewsItemModel();

            //default values
            model.Published = true;
            model.AllowComments = true;
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(NewsItemModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var newsItem = await _newsViewModelService.InsertNewsItemModel(model);
                Success(_translationService.GetResource("Admin.Content.News.NewsItems.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = newsItem.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            ViewBag.AllLanguages = _languageService.GetAllLanguages(true);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var newsItem = await _newsService.GetNewsById(id);
            if (newsItem == null)
                //No news item found with the specified id
                return RedirectToAction("List");

            ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);
            var model = newsItem.ToModel(_dateTimeService);
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Title = newsItem.GetTranslation(x => x.Title, languageId, false);
                locale.Short = newsItem.GetTranslation(x => x.Short, languageId, false);
                locale.Full = newsItem.GetTranslation(x => x.Full, languageId, false);
                locale.MetaKeywords = newsItem.GetTranslation(x => x.MetaKeywords, languageId, false);
                locale.MetaDescription = newsItem.GetTranslation(x => x.MetaDescription, languageId, false);
                locale.MetaTitle = newsItem.GetTranslation(x => x.MetaTitle, languageId, false);
                locale.SeName = newsItem.GetSeName(languageId, false);
            });
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(NewsItemModel model, bool continueEditing)
        {
            var newsItem = await _newsService.GetNewsById(model.Id);
            if (newsItem == null)
                //No news item found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                newsItem = await _newsViewModelService.UpdateNewsItemModel(newsItem, model);
                Success(_translationService.GetResource("Admin.Content.News.NewsItems.Updated"));

                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = newsItem.Id });
                }
                return RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var newsItem = await _newsService.GetNewsById(id);
            if (newsItem == null)
                //No news item found with the specified id
                return RedirectToAction("List");
            if (ModelState.IsValid)
            {
                await _newsService.DeleteNews(newsItem);

                Success(_translationService.GetResource("Admin.Content.News.NewsItems.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = newsItem.Id });
        }

        #endregion

        #region Comments

        public IActionResult Comments(string filterByNewsItemId)
        {
            ViewBag.FilterByNewsItemId = filterByNewsItemId;
            return View();
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> Comments(string filterByNewsItemId, DataSourceRequest command)
        {
            var comments = await _newsViewModelService.PrepareNewsCommentModel(filterByNewsItemId, command.Page, command.PageSize);

            var gridModel = new DataSourceResult
            {
                Data = comments.newsCommentModels.ToList(),
                Total = comments.totalCount,
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> CommentDelete(NewsComment model)
        {
            if (ModelState.IsValid)
            {
                await _newsViewModelService.CommentDelete(model);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }
        #endregion
    }
}
