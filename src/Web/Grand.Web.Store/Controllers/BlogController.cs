using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Blogs;
using Grand.Domain.Permissions;
using Grand.Domain.Seo;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BaseBlogController (ARCH-001 Blog consolidation). All regions of
// behavior live in the shared base; this class only supplies Store's DI wiring, the
// EditWarningCheck hook, the kept Preview action (Admin has no equivalent), and the attributes that
// used to arrive transitively via BaseStoreController. Same pattern as CategoryController.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
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
        storeService, dateTimeService, pictureViewModelService, seoSettings, postScope, categoryScope)
{
    // Re-derived from the original Store BlogController.Edit(GET) - the condition is unusual (warns
    // when NOT limited to stores at all, or when limited AND the staff member's store is one of
    // several) and easy to get backwards. Identical shape to CategoryController's own
    // EditWarningCheck override - same underlying idiom, different entity.
    protected override void EditWarningCheck(BlogPost blogPost)
    {
        if (!blogPost.LimitedToStores ||
            (blogPost.Stores.Contains(PostScope.DefaultStoreId) &&
             blogPost.Stores.Count > 1))
            Warning(TranslationService.GetResource("Admin.Content.Blog.BlogPosts.Permissions"));
    }

    // Admin has no equivalent action - a genuine Store-only addition, kept on the concrete subclass
    // rather than the shared base.
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Preview(string id)
    {
        var blogPost = await blogService.GetBlogPostById(id);
        if (blogPost == null) return RedirectToAction("List");
        if (!await PostScope.HasAccess(blogPost)) return RedirectToAction("List");

        var model = blogPost.ToModel(dateTimeService);
        return View(model);
    }
}
