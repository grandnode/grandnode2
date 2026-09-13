using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.News;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseNewsController (ARCH-001 News consolidation). All shared
// behavior lives in the base; this class only supplies Admin's DI wiring plus the attributes that
// used to arrive transitively via BaseAdminController - BaseNewsController can't inherit any single
// host's base controller. Same pattern as CategoryController (see that file).
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
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
        storeService, dateTimeService, scope);
