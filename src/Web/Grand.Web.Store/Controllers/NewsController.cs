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

    public async Task<IActionResult> List()
    {
        var model = new NewsItemListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, NewsItemListModel model)
    {
        var newsSettings = await _settingService.LoadSettingAsync<NewsSettings>(_contextAccessor.StoreContext.CurrentStore.Id);
        var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
        
        // Use the store-specific news service method with limit
        var news = await _newsService.GetStoreNews(storeId, command.Page - 1, command.PageSize, 
            newsSettings.StoreNewsLimit, model.SearchNewsTitle);
        
        var gridModel = new DataSourceResult 
        {
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

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Preview(string id)
    {
        var newsItem = await _newsService.GetNewsById(id);
        if (newsItem == null)
            return RedirectToAction("List");

        var model = newsItem.ToModel(_dateTimeService);
        return View(model);
    }

    #endregion
}