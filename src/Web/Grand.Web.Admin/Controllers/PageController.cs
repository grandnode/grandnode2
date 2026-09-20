using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Pages;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class PageController(
    IPageViewModelService pageViewModelService,
    IPageService pageService,
    ILanguageService languageService,
    ITranslationService translationService,
    IDateTimeService dateTimeService,
    IAdminDataScope<Page> scope)
    : BasePageController(pageViewModelService, pageService, languageService, translationService,
        dateTimeService, scope);
