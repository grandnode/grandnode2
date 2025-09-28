using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Blogs;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.Blog)]
public class BlogController : BaseStoreController
{
    #region Constructors

    public BlogController(
        IBlogService blogService,
        IBlogViewModelService blogViewModelService,
        ILanguageService languageService,
        ITranslationService translationService,
        IStoreService storeService,
        IContextAccessor contextAccessor,
        IGroupService groupService,
        IDateTimeService dateTimeService)
    {
        _blogService = blogService;
        _blogViewModelService = blogViewModelService;
        _languageService = languageService;
        _translationService = translationService;
        _storeService = storeService;
        _contextAccessor = contextAccessor;
        _groupService = groupService;
        _dateTimeService = dateTimeService;
    }

    #endregion

    #region Fields

    private readonly IBlogService _blogService;
    private readonly IBlogViewModelService _blogViewModelService;
    private readonly ILanguageService _languageService;
    private readonly ITranslationService _translationService;
    private readonly IStoreService _storeService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IGroupService _groupService;
    private readonly IDateTimeService _dateTimeService;

    #endregion

    #region Blog posts

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        return View();
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        var blogPosts = await _blogViewModelService.PrepareBlogPostsModel(command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = blogPosts.blogPosts,
            Total = blogPosts.totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);
        var model = new BlogPostModel
        {
            //default values
            AllowComments = true,
            CreateDate = DateTime.UtcNow
        };

        //locales
        await AddLocales(_languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(BlogPostModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            model.Stores = [_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId];
            var blogPost = await _blogViewModelService.InsertBlogPostModel(model);
            Success(_translationService.GetResource("Admin.Content.Blog.BlogPosts.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = blogPost.Id }) : RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var blogPost = await _blogService.GetBlogPostById(id);
        if (blogPost == null)
            //No blog post found with the specified id
            return RedirectToAction("List");

        if (!blogPost.LimitedToStores || (blogPost.LimitedToStores &&
                                          blogPost.Stores.Contains(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) &&
                                          blogPost.Stores.Count > 1))
        {
            Warning(_translationService.GetResource("Admin.Content.Blog.BlogPosts.Permissions"));
        }
        else
        {
            if (!blogPost.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
                return RedirectToAction("List");
        }

        ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);
        var model = blogPost.ToModel(_dateTimeService);

        //locales
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
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
        var blogPost = await _blogService.GetBlogPostById(model.Id);
        if (blogPost == null)
            //No blog post found with the specified id
            return RedirectToAction("List");

        if (!blogPost.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            return RedirectToAction("Edit", new { id = blogPost.Id });

        if (ModelState.IsValid)
        {
            model.Stores = [_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId];
            blogPost = await _blogViewModelService.UpdateBlogPostModel(model, blogPost);
            Success(_translationService.GetResource("Admin.Content.Blog.BlogPosts.Updated"));

            if (continueEditing)
            {
                //selected tab
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = blogPost.Id });
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
        var blogPost = await _blogService.GetBlogPostById(id);
        if (blogPost == null)
            //No blog post found with the specified id
            return RedirectToAction("List");

        if (!blogPost.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            return RedirectToAction("Edit", new { id = blogPost.Id });

        if (ModelState.IsValid)
        {
            await _blogService.DeleteBlogPost(blogPost);
            Success(_translationService.GetResource("Admin.Content.Blog.BlogPosts.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Preview(string id)
    {
        var blogPost = await _blogService.GetBlogPostById(id);
        if (blogPost == null)
            return RedirectToAction("List");

        if (!blogPost.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            return RedirectToAction("List");

        var model = blogPost.ToModel(_dateTimeService);
        return View(model);
    }

    #endregion
}