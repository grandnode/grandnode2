using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Blogs;
using Grand.Domain.Permissions;
using Grand.Domain.Seo;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Blogs;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Controllers;

// [AutoValidateAntiforgeryToken] restated on each concrete host subclass too (Task 8) - ASP.NET Core
// resolves filters from the concrete controller's full type hierarchy at runtime, so every real
// endpoint is already protected. Added here as well, mirroring BaseProductController/
// BaseCategoryController.
[PermissionAuthorize(PermissionSystemName.Blog)]
[AutoValidateAntiforgeryToken]
public abstract class BaseBlogController(
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
    : BaseController
{
    protected const string NoAccessToBlogPostMessage = "You don't have access to this blog post";
    protected const string NoAccessToBlogCategoryMessage = "You don't have access to this blog category";
    protected const string CategoryListAction = "CategoryList";

    /// <summary>Hook for host-specific UI-copy warnings on BlogPost Edit(GET) that aren't access-scope
    /// decisions. Overridden by the Store subclass (Task 8); no-op everywhere else. Mirrors
    /// BaseCategoryController.EditWarningCheck, and re-derives the identical condition Store's
    /// original Category Edit(GET) already used.</summary>
    protected virtual void EditWarningCheck(BlogPost blogPost) { }

    // Exposed for host subclasses: primary-constructor parameters are not visible to derived classes
    // by name in C#.
    protected ITranslationService TranslationService => translationService;
    protected IAdminDataScope<BlogPost> PostScope => postScope;
    protected IAdminDataScope<BlogCategory> CategoryScope => categoryScope;

    #region Blog posts

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List() => View();

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        // PrepareBlogPostsModel already reads StaffStoreId from ambient IContextAccessor and passes
        // it into IBlogService.GetAllBlogPosts - Admin's empty StaffStoreId means unscoped, Store's
        // means scoped, with zero controller involvement. Confirmed in the design spec §4 - no
        // SearchStoreId forcing needed here, unlike every other entity in this initiative.
        var blogPosts = await blogViewModelService.PrepareBlogPostsModel(command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = blogPosts.blogPosts,
            Total = blogPosts.totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.AllLanguages = await languageService.GetAllLanguages(true);
        var model = new BlogPostModel {
            AllowComments = true,
            CreateDate = DateTime.UtcNow
        };
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(BlogPostModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (postScope.DefaultStoreId is not null) model.Stores = [postScope.DefaultStoreId];
            var blogPost = await blogViewModelService.InsertBlogPostModel(model);
            Success(translationService.GetResource("Admin.Content.Blog.BlogPosts.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = blogPost.Id }) : RedirectToAction("List");
        }

        ViewBag.AllLanguages = await languageService.GetAllLanguages(true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var blogPost = await blogService.GetBlogPostById(id);
        if (blogPost == null) return RedirectToAction("List");

        // Warning (loose UI copy) vs deny (CanView, strict) - same split as Category's Edit(GET).
        // Admin: no-op EditWarningCheck, CanView always true -> unaffected.
        EditWarningCheck(blogPost);
        if (!await postScope.CanView(blogPost)) return RedirectToAction("List");

        ViewBag.AllLanguages = await languageService.GetAllLanguages(true);
        var model = blogPost.ToModel(dateTimeService);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Title = blogPost.GetTranslation(x => x.Title, languageId, false);
            locale.Body = blogPost.GetTranslation(x => x.Body, languageId, false);
            locale.BodyOverview = blogPost.GetTranslation(x => x.BodyOverview, languageId, false);
            locale.MetaKeywords = blogPost.GetTranslation(x => x.MetaKeywords, languageId, false);
            locale.MetaDescription = blogPost.GetTranslation(x => x.MetaDescription, languageId, false);
            locale.MetaTitle = blogPost.GetTranslation(x => x.MetaTitle, languageId, false);
            locale.SeName = blogPost.GetSeName(languageId, false);
        });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(BlogPostModel model, bool continueEditing)
    {
        var blogPost = await blogService.GetBlogPostById(model.Id);
        if (blogPost == null) return RedirectToAction("List");
        if (!await postScope.HasAccess(blogPost)) return RedirectToAction("Edit", new { id = blogPost.Id });

        if (ModelState.IsValid)
        {
            if (postScope.DefaultStoreId is not null) model.Stores = [postScope.DefaultStoreId];
            blogPost = await blogViewModelService.UpdateBlogPostModel(model, blogPost);
            Success(translationService.GetResource("Admin.Content.Blog.BlogPosts.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = blogPost.Id });
            }
            return RedirectToAction("List");
        }

        ViewBag.AllLanguages = await languageService.GetAllLanguages(true);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Title = blogPost.GetTranslation(x => x.Title, languageId, false);
            locale.Body = blogPost.GetTranslation(x => x.Body, languageId, false);
            locale.BodyOverview = blogPost.GetTranslation(x => x.BodyOverview, languageId, false);
            locale.MetaKeywords = blogPost.GetTranslation(x => x.MetaKeywords, languageId, false);
            locale.MetaDescription = blogPost.GetTranslation(x => x.MetaDescription, languageId, false);
            locale.MetaTitle = blogPost.GetTranslation(x => x.MetaTitle, languageId, false);
            locale.SeName = blogPost.GetSeName(languageId, false);
        });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var blogPost = await blogService.GetBlogPostById(id);
        if (blogPost == null) return RedirectToAction("List");
        if (!await postScope.HasAccess(blogPost)) return RedirectToAction("Edit", new { id });

        if (ModelState.IsValid)
        {
            await blogService.DeleteBlogPost(blogPost);
            Success(translationService.GetResource("Admin.Content.Blog.BlogPosts.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id });
    }

    #endregion

    #region Picture

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> PicturePopup(string blogpostId)
    {
        var blogpost = await blogService.GetBlogPostById(blogpostId);
        if (blogpost == null) return Content("Blog post not exist");
        if (!await postScope.HasAccess(blogpost)) return Content(NoAccessToBlogPostMessage);
        if (string.IsNullOrEmpty(blogpost.PictureId)) return Content("Picture not exist");

        return View("Partials/PicturePopup",
            await pictureViewModelService.PreparePictureModel(blogpost.PictureId, blogpost.Id));
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> PicturePopup(PictureModel model)
    {
        if (ModelState.IsValid)
        {
            var blogpost = await blogService.GetBlogPostById(model.ObjectId);
            if (blogpost == null) throw new ArgumentException("No blog post found with the specified id");
            if (!await postScope.HasAccess(blogpost)) return Content(NoAccessToBlogPostMessage);
            if (string.IsNullOrEmpty(blogpost.PictureId)) throw new ArgumentException("No picture found with the specified id");
            if (blogpost.PictureId != model.Id) throw new ArgumentException("Picture ident doesn't fit with blog post");

            await pictureViewModelService.UpdatePicture(model);
            return Content("");
        }

        Error(ModelState);
        return View("Partials/PicturePopup", model);
    }

    #endregion

    #region Categories

    public IActionResult CategoryList() => View();

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> CategoryList(DataSourceRequest command)
    {
        var categories = await blogService.GetAllBlogCategories(categoryScope.DefaultStoreId ?? "");
        var gridModel = new DataSourceResult {
            Data = categories,
            Total = categories.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> CategoryCreate()
    {
        ViewBag.AllLanguages = await languageService.GetAllLanguages(true);
        var model = new BlogCategoryModel();
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> CategoryCreate(BlogCategoryModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (categoryScope.DefaultStoreId is not null) model.Stores = [categoryScope.DefaultStoreId];
            var blogCategory = model.ToEntity();
            blogCategory.SeName = SeoExtensions.GetSeName(
                string.IsNullOrEmpty(blogCategory.SeName) ? blogCategory.Name : blogCategory.SeName,
                seoSettings.ConvertNonWesternChars, seoSettings.AllowUnicodeCharsInUrls, seoSettings.SeoCharConversion);

            await blogService.InsertBlogCategory(blogCategory);
            Success(translationService.GetResource("Admin.Content.Blog.BlogCategory.Added"));
            return continueEditing
                ? RedirectToAction("CategoryEdit", new { id = blogCategory.Id })
                : RedirectToAction(CategoryListAction);
        }

        ViewBag.AllLanguages = await languageService.GetAllLanguages(true);
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> CategoryEdit(string id)
    {
        var blogCategory = await blogService.GetBlogCategoryById(id);
        if (blogCategory == null) return RedirectToAction(CategoryListAction);
        if (!await categoryScope.HasAccess(blogCategory)) return RedirectToAction(CategoryListAction);

        ViewBag.AllLanguages = await languageService.GetAllLanguages(true);
        var model = blogCategory.ToModel();
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = blogCategory.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> CategoryEdit(BlogCategoryModel model, bool continueEditing)
    {
        var blogCategory = await blogService.GetBlogCategoryById(model.Id);
        if (blogCategory == null) return RedirectToAction(CategoryListAction);
        if (!await categoryScope.HasAccess(blogCategory)) return RedirectToAction(CategoryListAction);

        if (ModelState.IsValid)
        {
            if (categoryScope.DefaultStoreId is not null) model.Stores = [categoryScope.DefaultStoreId];
            blogCategory = model.ToEntity(blogCategory);
            blogCategory.SeName = SeoExtensions.GetSeName(
                string.IsNullOrEmpty(blogCategory.SeName) ? blogCategory.Name : blogCategory.SeName,
                seoSettings.ConvertNonWesternChars, seoSettings.AllowUnicodeCharsInUrls, seoSettings.SeoCharConversion);
            await blogService.UpdateBlogCategory(blogCategory);
            Success(translationService.GetResource("Admin.Content.Blog.BlogCategory.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("CategoryEdit", new { id = blogCategory.Id });
            }
            return RedirectToAction(CategoryListAction);
        }

        ViewBag.AllLanguages = await languageService.GetAllLanguages(true);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = blogCategory.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> CategoryDelete(string id)
    {
        var blogCategory = await blogService.GetBlogCategoryById(id);
        if (blogCategory == null) return RedirectToAction(CategoryListAction);
        if (!await categoryScope.HasAccess(blogCategory)) return RedirectToAction(CategoryListAction);

        if (ModelState.IsValid)
        {
            await blogService.DeleteBlogCategory(blogCategory);
            Success(translationService.GetResource("Admin.Content.Blog.BlogCategory.Deleted"));
            return RedirectToAction(CategoryListAction);
        }

        Error(ModelState);
        return RedirectToAction("CategoryEdit", new { id = blogCategory.Id });
    }

    #endregion
}
