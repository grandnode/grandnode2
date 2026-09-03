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

// Reduced to a thin subclass of BaseNewsController (ARCH-001 News consolidation). All shared
// behavior lives in the base; this class supplies Store's DI wiring, the EditWarningCheck hook, and
// the kept Preview action (Admin has no equivalent). Same pattern as CategoryController's
// EditWarningCheck override (see that file).
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
    // Re-derived from the original Store NewsController.Edit(GET) - the condition is unusual (warns
    // when NOT limited to stores at all, or when limited AND the staff member's store is one of
    // several) and easy to get backwards. Fourth occurrence of this exact idiom in ARCH-001
    // (Category, Blog, Page, now News) - treat as proven.
    protected override void EditWarningCheck(NewsItem newsItem)
    {
        if (!newsItem.LimitedToStores ||
            (newsItem.Stores.Contains(Scope.DefaultStoreId) &&
             newsItem.Stores.Count > 1))
            Warning(TranslationService.GetResource("Admin.Content.News.Permissions"));
    }

    // Admin has no equivalent action - a genuine Store-only addition, kept on the concrete subclass
    // rather than the shared base.
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Preview(string id)
    {
        var newsItem = await newsService.GetNewsById(id);
        if (newsItem == null) return RedirectToAction("List");
        if (!await Scope.HasAccess(newsItem)) return RedirectToAction("List");

        var model = newsItem.ToModel(dateTimeService);
        return View(model);
    }
}
