using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.News;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.News;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.News)]
public class NewsController : BaseStoreController
{
    #region Constructors

    public NewsController(
        INewsViewModelService newsViewModelService,
        INewsService newsService,
        ILanguageService languageService,
        ITranslationService translationService,
        ISettingService settingService,
        IDateTimeService dateTimeService,
        IContextAccessor contextAccessor)
    {
        _newsViewModelService = newsViewModelService;
        _newsService = newsService;
        _languageService = languageService;
        _translationService = translationService;
        _settingService = settingService;
        _dateTimeService = dateTimeService;
        _contextAccessor = contextAccessor;
    }

    #endregion

    #region Fields

    private readonly INewsViewModelService _newsViewModelService;
    private readonly INewsService _newsService;
    private readonly ILanguageService _languageService;
    private readonly ITranslationService _translationService;
    private readonly ISettingService _settingService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IContextAccessor _contextAccessor;

    #endregion

    #region News items

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        var model = new NewsItemListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, NewsItemListModel model)
    {
        var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
        var newsSettings = await _settingService.LoadSetting<NewsSettings>(storeId);

        var news = await _newsService.GetAllNews(storeId, command.Page - 1, command.PageSize, newsTitle: model.SearchNewsTitle);

        var gridModel = new DataSourceResult {
            Data = news.Select(x =>
            {
                var m = x.ToModel(_dateTimeService);
                m.Full = "";
                m.CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
                m.Comments = x.CommentCount;
                return m;
            }).ToList(),
            Total = news.TotalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.AllLanguages = _languageService.GetAllLanguages(true);
        var model = new NewsItemModel {
            //default values
            Published = true,
            AllowComments = true
        };

        //locales
        await AddLocales(_languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(NewsItemModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            model.Stores = [_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId];
            var newsItem = await _newsViewModelService.InsertNewsItemModel(model);
            await _newsService.UpdateNews(newsItem);

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

        if (!newsItem.LimitedToStores || (newsItem.LimitedToStores &&
                                          newsItem.Stores.Contains(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) &&
                                          newsItem.Stores.Count > 1))
        {
            Warning(_translationService.GetResource("Admin.Content.News.Permissions"));
        }
        else
        {
            if (!newsItem.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
                return RedirectToAction("List");
        }

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
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(NewsItemModel model, bool continueEditing)
    {
        var newsItem = await _newsService.GetNewsById(model.Id);
        if (newsItem == null)
            //No news item found with the specified id
            return RedirectToAction("List");

        if (!newsItem.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            return RedirectToAction("Edit", new { id = newsItem.Id });

        if (ModelState.IsValid)
        {
            model.Stores = [_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId];
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

        if (!newsItem.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
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

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Preview(string id)
    {
        var newsItem = await _newsService.GetNewsById(id);
        if (newsItem == null)
            return RedirectToAction("List");

        if (!newsItem.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            return RedirectToAction("List");

        var model = newsItem.ToModel(_dateTimeService);
        return View(model);
    }

    #endregion
}