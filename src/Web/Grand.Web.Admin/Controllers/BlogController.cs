using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Blogs;
using Grand.Domain.Seo;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseBlogController (ARCH-001 Blog consolidation). All regions of
// behavior live in the shared base; this class only supplies Admin's DI wiring plus the attributes
// that used to arrive transitively via BaseAdminController - BaseBlogController can't inherit any
// single host's base controller. Same pattern as CategoryController (see that file).
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class BlogController(
    IBlogService blogService,
    IBlogViewModelService blogViewModelService,
    ILanguageService languageService,
    ITranslationService translationService,
    IStoreService storeService,
    IDateTimeService dateTimeService,
    IPictureViewModelService pictureViewModelService,
    SeoSettings seoSettings,
    IAdminDataScope<BlogPost> postScope,
    IAdminDataScope<BlogCategory> categoryScope)
    : BaseBlogController(blogService, blogViewModelService, languageService, translationService,
        storeService, dateTimeService, pictureViewModelService, seoSettings, postScope, categoryScope);
