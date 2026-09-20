using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.News;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class NewsController(
    INewsViewModelService newsViewModelService,
    INewsService newsService,
    ILanguageService languageService,
    ITranslationService translationService,
    IStoreService storeService,
    IDateTimeService dateTimeService,
    IAdminDataScope<NewsItem> scope)
    : BaseNewsController(newsViewModelService, newsService, languageService, translationService,
        storeService, dateTimeService, scope)
{
    protected override void EditWarningCheck(NewsItem newsItem)
    {
        if (!newsItem.LimitedToStores ||
            (newsItem.Stores.Contains(Scope.DefaultStoreId) &&
             newsItem.Stores.Count > 1))
            Warning(TranslationService.GetResource("Admin.Content.News.Permissions"));
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Preview(string id)
    {
        var newsItem = await NewsService.GetNewsById(id);
        if (newsItem == null) return RedirectToAction("List");
        if (!await Scope.HasAccess(newsItem)) return RedirectToAction("List");

        var model = newsItem.ToModel(DateTimeService);
        return View(model);
    }
}
