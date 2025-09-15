using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Permissions;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Blogs;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.Blog)]
public class BlogController : BaseStoreController
{
    #region Constants
    
    private const int StorePageSizeLimit = 50; // Store limit: maximum 50 items per page
    
    #endregion
    
    #region Constructors

    public BlogController(
        IBlogService blogService,
        IBlogViewModelService blogViewModelService,
        ILanguageService languageService,
        ITranslationService translationService,
        IStoreService storeService,
        IContextAccessor contextAccessor,
        IGroupService groupService,
        IDateTimeService dateTimeService,
        IPictureViewModelService pictureViewModelService,
        SeoSettings seoSettings)
    {
        _blogService = blogService;
        _blogViewModelService = blogViewModelService;
        _languageService = languageService;
        _translationService = translationService;
        _storeService = storeService;
        _contextAccessor = contextAccessor;
        _groupService = groupService;
        _dateTimeService = dateTimeService;
        _pictureViewModelService = pictureViewModelService;
        _seoSettings = seoSettings;
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
    private readonly IPictureViewModelService _pictureViewModelService;
    private readonly SeoSettings _seoSettings;

    #endregion

    #region Helper Methods
    
    /// <summary>
    /// Apply store-specific page size limit
    /// </summary>
    /// <param name="requestedPageSize">Requested page size</param>
    /// <returns>Limited page size</returns>
    private int ApplyStorePageSizeLimit(int requestedPageSize)
    {
        return Math.Min(requestedPageSize, StorePageSizeLimit);
    }
    
    /// <summary>
    /// Get current store ID for filtering
    /// </summary>
    /// <returns>Current store ID</returns>
    private string GetCurrentStoreId()
    {
        return _contextAccessor.WorkContext.CurrentStore.Id;
    }

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
        // Apply store limits - limit page size and filter by current store
        var limitedPageSize = ApplyStorePageSizeLimit(command.PageSize);
        var currentStoreId = GetCurrentStoreId();
        
        // Get blog posts filtered by current store
        var blogPosts = await _blogService.GetAllBlogPosts(
            storeId: currentStoreId,
            pageIndex: command.Page - 1, 
            pageSize: limitedPageSize, 
            showHidden: true);
            
        var blogPostModels = blogPosts.Select(x =>
        {
            var m = x.ToModel(_dateTimeService);
            m.Body = "";
            if (x.StartDateUtc.HasValue)
                m.StartDate = _dateTimeService.ConvertToUserTime(x.StartDateUtc.Value, DateTimeKind.Utc);
            if (x.EndDateUtc.HasValue)
                m.EndDate = _dateTimeService.ConvertToUserTime(x.EndDateUtc.Value, DateTimeKind.Utc);
            m.CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
            m.Comments = x.CommentCount;
            return m;
        });
        
        var gridModel = new DataSourceResult {
            Data = blogPostModels,
            Total = blogPosts.TotalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);
        var model = new BlogPostModel {
            //default values
            AllowComments = true
        };
        
        // Limit to current store only
        var currentStoreId = GetCurrentStoreId();
        model.Stores = [currentStoreId];
        
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
            // Force store limitation - only allow current store
            var currentStoreId = GetCurrentStoreId();
            model.Stores = [currentStoreId];
            
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

        // Store limitation - check if blog post belongs to current store
        var currentStoreId = GetCurrentStoreId();
        if (!blogPost.AccessToEntityByStore(currentStoreId))
        {
            Warning(_translationService.GetResource("Admin.Content.Blog.BlogPosts.Permissions"));
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

        // Store limitation - check if blog post belongs to current store
        var currentStoreId = GetCurrentStoreId();
        if (!blogPost.AccessToEntityByStore(currentStoreId))
            return RedirectToAction("Edit", new { id = blogPost.Id });

        if (ModelState.IsValid)
        {
            // Force store limitation - only allow current store
            model.Stores = [currentStoreId];

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

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var blogPost = await _blogService.GetBlogPostById(id);
        if (blogPost == null)
            //No blog post found with the specified id
            return RedirectToAction("List");

        // Store limitation - check if blog post belongs to current store
        var currentStoreId = GetCurrentStoreId();
        if (!blogPost.AccessToEntityByStore(currentStoreId))
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

    #endregion

    #region Picture

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> PicturePopup(string blogpostId)
    {
        var blogpost = await _blogService.GetBlogPostById(blogpostId);
        if (blogpost == null)
            return Content("Blog post not exist");

        // Store limitation - check if blog post belongs to current store
        var currentStoreId = GetCurrentStoreId();
        if (!blogpost.AccessToEntityByStore(currentStoreId))
            return Content("Access denied");

        if (string.IsNullOrEmpty(blogpost.PictureId))
            return Content("Picture not exist");

        return View("Partials/PicturePopup",
            await _pictureViewModelService.PreparePictureModel(blogpost.PictureId, blogpost.Id));
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> PicturePopup(PictureModel model)
    {
        if (ModelState.IsValid)
        {
            var blogpost = await _blogService.GetBlogPostById(model.ObjectId);
            if (blogpost == null)
                throw new ArgumentException("No blog post found with the specified id");

            // Store limitation - check if blog post belongs to current store
            var currentStoreId = GetCurrentStoreId();
            if (!blogpost.AccessToEntityByStore(currentStoreId))
                throw new ArgumentException("Access denied");

            if (string.IsNullOrEmpty(blogpost.PictureId))
                throw new ArgumentException("No picture found with the specified id");

            if (blogpost.PictureId != model.Id)
                throw new ArgumentException("Picture ident doesn't fit with blog post");

            await _pictureViewModelService.UpdatePicture(model);

            return Content("");
        }

        Error(ModelState);

        return View("Partials/PicturePopup", model);
    }

    #endregion

    #region Categories

    public IActionResult CategoryList()
    {
        return View();
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> CategoryList(DataSourceRequest command)
    {
        // Store limitation - filter by current store
        var currentStoreId = GetCurrentStoreId();
        var categories = await _blogService.GetAllBlogCategories(currentStoreId);
        
        var gridModel = new DataSourceResult {
            Data = categories,
            Total = categories.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> CategoryCreate()
    {
        ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);
        var model = new BlogCategoryModel();
        
        // Limit to current store only
        var currentStoreId = GetCurrentStoreId();
        model.Stores = [currentStoreId];
        
        //locales
        await AddLocales(_languageService, model.Locales);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> CategoryCreate(BlogCategoryModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            // Force store limitation - only allow current store
            var currentStoreId = GetCurrentStoreId();
            model.Stores = [currentStoreId];

            var blogCategory = model.ToEntity();
            blogCategory.SeName = SeoExtensions.GetSeName(
                string.IsNullOrEmpty(blogCategory.SeName) ? blogCategory.Name : blogCategory.SeName,
                _seoSettings.ConvertNonWesternChars, _seoSettings.AllowUnicodeCharsInUrls,
                _seoSettings.SeoCharConversion);

            await _blogService.InsertBlogCategory(blogCategory);
            Success(_translationService.GetResource("Admin.Content.Blog.BlogCategory.Added"));
            return continueEditing
                ? RedirectToAction("CategoryEdit", new { id = blogCategory.Id })
                : RedirectToAction("CategoryList");
        }

        //If we got this far, something failed, redisplay form
        ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);
        //locales
        await AddLocales(_languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> CategoryEdit(string id)
    {
        var blogCategory = await _blogService.GetBlogCategoryById(id);
        if (blogCategory == null)
            //No blog category found with the specified id
            return RedirectToAction("CategoryList");

        // Store limitation - check if blog category belongs to current store
        var currentStoreId = GetCurrentStoreId();
        if (!blogCategory.AccessToEntityByStore(currentStoreId))
        {
            Warning(_translationService.GetResource("Admin.Content.Blog.BlogCategory.Permissions"));
            return RedirectToAction("CategoryList");
        }

        ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);
        var model = blogCategory.ToModel();
        //locales
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
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
        var blogCategory = await _blogService.GetBlogCategoryById(model.Id);
        if (blogCategory == null)
            //No blog category found with the specified id
            return RedirectToAction("CategoryList");

        // Store limitation - check if blog category belongs to current store
        var currentStoreId = GetCurrentStoreId();
        if (!blogCategory.AccessToEntityByStore(currentStoreId))
            return RedirectToAction("CategoryEdit", new { id = blogCategory.Id });

        if (ModelState.IsValid)
        {
            // Force store limitation - only allow current store
            model.Stores = [currentStoreId];

            blogCategory = model.ToEntity(blogCategory);
            blogCategory.SeName = SeoExtensions.GetSeName(
                string.IsNullOrEmpty(blogCategory.SeName) ? blogCategory.Name : blogCategory.SeName,
                _seoSettings.ConvertNonWesternChars, _seoSettings.AllowUnicodeCharsInUrls,
                _seoSettings.SeoCharConversion);
            await _blogService.UpdateBlogCategory(blogCategory);
            Success(_translationService.GetResource("Admin.Content.Blog.BlogCategory.Updated"));
            if (continueEditing)
            {
                //selected tab
                await SaveSelectedTabIndex();

                return RedirectToAction("CategoryEdit", new { id = blogCategory.Id });
            }

            return RedirectToAction("CategoryList");
        }

        //If we got this far, something failed, redisplay form
        ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);

        //locales
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = blogCategory.GetTranslation(x => x.Name, languageId, false);
        });

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> CategoryDelete(string id)
    {
        var blogcategory = await _blogService.GetBlogCategoryById(id);
        if (blogcategory == null)
            //No blog category found with the specified id
            return RedirectToAction("CategoryList");

        // Store limitation - check if blog category belongs to current store
        var currentStoreId = GetCurrentStoreId();
        if (!blogcategory.AccessToEntityByStore(currentStoreId))
            return RedirectToAction("CategoryEdit", new { id = blogcategory.Id });

        if (ModelState.IsValid)
        {
            await _blogService.DeleteBlogCategory(blogcategory);

            Success(_translationService.GetResource("Admin.Content.Blog.BlogCategory.Deleted"));
            return RedirectToAction("CategoryList");
        }

        Error(ModelState);
        return RedirectToAction("CategoryEdit", new { id = blogcategory.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> CategoryPostList(string categoryId)
    {
        var blogCategory = await _blogService.GetBlogCategoryById(categoryId);
        if (blogCategory == null)
            return ErrorForKendoGridJson("blogCategory no exists");

        // Store limitation - check if blog category belongs to current store
        var currentStoreId = GetCurrentStoreId();
        if (!blogCategory.AccessToEntityByStore(currentStoreId))
            return ErrorForKendoGridJson("blogCategory no permission");

        var blogposts = new List<BlogCategoryPost>();
        foreach (var item in blogCategory.BlogPosts)
        {
            var post = new BlogCategoryPost {
                Id = item.Id,
                BlogPostId = item.BlogPostId
            };
            var _post = await _blogService.GetBlogPostById(item.BlogPostId);
            if (_post != null)
                post.Name = _post.Title;

            blogposts.Add(post);
        }

        var gridModel = new DataSourceResult {
            Data = blogposts,
            Total = blogCategory.BlogPosts.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> CategoryPostDelete(string categoryId, string id)
    {
        var blogCategory = await _blogService.GetBlogCategoryById(categoryId);
        if (blogCategory == null)
            return ErrorForKendoGridJson("blogCategory no exists");

        // Store limitation - check if blog category belongs to current store
        var currentStoreId = GetCurrentStoreId();
        if (!blogCategory.AccessToEntityByStore(currentStoreId))
            return ErrorForKendoGridJson("blogCategory no permission");

        if (ModelState.IsValid)
        {
            var post = blogCategory.BlogPosts.FirstOrDefault(x => x.Id == id);
            if (post != null)
            {
                blogCategory.BlogPosts.Remove(post);
                await _blogService.UpdateBlogCategory(blogCategory);
            }

            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> BlogPostAddPopup(string categoryId)
    {
        var model = new AddBlogPostCategoryModel();
        // Store limitation - only show current store
        var currentStoreId = GetCurrentStoreId();
        var currentStore = await _storeService.GetStoreById(currentStoreId);

        model.AvailableStores.Add(new SelectListItem { Text = currentStore.Shortcut, Value = currentStore.Id });
        model.CategoryId = categoryId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BlogPostAddPopupList(DataSourceRequest command, AddBlogPostCategoryModel model)
    {
        var gridModel = new DataSourceResult();

        // Store limitation - filter by current store and apply page size limit
        var currentStoreId = GetCurrentStoreId();
        var limitedPageSize = ApplyStorePageSizeLimit(command.PageSize);

        var posts = await _blogService.GetAllBlogPosts(currentStoreId, 
            blogPostName: model.SearchBlogTitle,
            pageIndex: command.Page - 1, 
            pageSize: limitedPageSize);
        gridModel.Data = posts.Select(x => new { x.Id, Name = x.Title });
        gridModel.Total = posts.TotalCount;

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BlogPostAddPopup(AddBlogPostCategoryModel model)
    {
        if (model.SelectedBlogPostIds != null)
        {
            var blogCategory = await _blogService.GetBlogCategoryById(model.CategoryId);
            if (blogCategory != null)
            {
                // Store limitation - check if blog category belongs to current store
                var currentStoreId = GetCurrentStoreId();
                if (!blogCategory.AccessToEntityByStore(currentStoreId))
                    return Content("Access denied");
                    
                foreach (var id in model.SelectedBlogPostIds)
                {
                    var post = await _blogService.GetBlogPostById(id);
                    if (post != null && post.AccessToEntityByStore(currentStoreId))
                        if (!blogCategory.BlogPosts.Any(x => x.BlogPostId == id))
                        {
                            blogCategory.BlogPosts.Add(new Domain.Blogs.BlogCategoryPost { BlogPostId = id });
                            await _blogService.UpdateBlogCategory(blogCategory);
                        }
                }
            }
        }

        return Content("");
    }

    #endregion

    #region Comments

    public IActionResult Comments(string filterByBlogPostId)
    {
        ViewBag.FilterByBlogPostId = filterByBlogPostId;
        return View();
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> Comments(string filterByBlogPostId, DataSourceRequest command)
    {
        // Store limitation - apply page size limit
        var limitedPageSize = ApplyStorePageSizeLimit(command.PageSize);
        
        var model = await _blogViewModelService.PrepareBlogPostCommentsModel(filterByBlogPostId, command.Page,
            limitedPageSize);
        
        // Additional store filtering for comments - only show comments from current store's blog posts
        var currentStoreId = GetCurrentStoreId();
        var filteredComments = new List<BlogCommentModel>();
        foreach (var comment in model.blogComments)
        {
            var blogPost = await _blogService.GetBlogPostById(comment.BlogPostId);
            if (blogPost != null && blogPost.AccessToEntityByStore(currentStoreId))
            {
                filteredComments.Add(comment);
            }
        }
        
        var gridModel = new DataSourceResult {
            Data = filteredComments,
            Total = filteredComments.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> CommentDelete(string id)
    {
        var comment = await _blogService.GetBlogCommentById(id);
        if (comment == null)
            throw new ArgumentException("No comment found with the specified id");

        var blogPost = await _blogService.GetBlogPostById(comment.BlogPostId);
        
        // Store limitation - check if blog post belongs to current store
        var currentStoreId = GetCurrentStoreId();
        if (!blogPost.AccessToEntityByStore(currentStoreId))
            return ErrorForKendoGridJson("blogPost no permission");

        if (ModelState.IsValid)
        {
            await _blogService.DeleteBlogComment(comment);
            //update totals
            var comments = await _blogService.GetBlogCommentsByBlogPostId(blogPost.Id);
            blogPost.CommentCount = comments.Count;
            await _blogService.UpdateBlogPost(blogPost);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Products

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> Products(string blogPostId, DataSourceRequest command)
    {
        // Store limitation - apply page size limit
        var limitedPageSize = ApplyStorePageSizeLimit(command.PageSize);
        
        var model = await _blogViewModelService.PrepareBlogProductsModel(blogPostId, command.Page, limitedPageSize);
        var gridModel = new DataSourceResult {
            Data = model.blogProducts,
            Total = model.totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAddPopup(string blogPostId)
    {
        // Store limitation - check if blog post belongs to current store
        var blogPost = await _blogService.GetBlogPostById(blogPostId);
        if (blogPost == null)
            return Content("Blog post not found");
            
        var currentStoreId = GetCurrentStoreId();
        if (!blogPost.AccessToEntityByStore(currentStoreId))
            return Content("Access denied");
            
        var model = await _blogViewModelService.PrepareBlogModelAddProductModel(blogPostId);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command,
        BlogProductModel.AddProductModel model)
    {
        // Store limitation - apply page size limit and filter by current store
        var limitedPageSize = ApplyStorePageSizeLimit(command.PageSize);
        var currentStoreId = GetCurrentStoreId();
        
        // Ensure the search is limited to current store
        model.SearchStoreId = currentStoreId;
        
        var products = await _blogViewModelService.PrepareProductModel(model, command.Page, limitedPageSize);

        var gridModel = new DataSourceResult {
            Data = products.products.ToList(),
            Total = products.totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopup(string blogPostId, BlogProductModel.AddProductModel model)
    {
        // Store limitation - check if blog post belongs to current store
        var blogPost = await _blogService.GetBlogPostById(blogPostId);
        if (blogPost == null)
            return Content("Blog post not found");
            
        var currentStoreId = GetCurrentStoreId();
        if (!blogPost.AccessToEntityByStore(currentStoreId))
            return Content("Access denied");
            
        if (model.SelectedProductIds != null) 
            await _blogViewModelService.InsertProductModel(blogPostId, model);
        return Content("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UpdateProduct(BlogProductModel model)
    {
        // Store limitation - check if blog post belongs to current store
        var blogPost = await _blogService.GetBlogPostById(model.BlogPostId);
        if (blogPost == null)
            return ErrorForKendoGridJson("Blog post not found");
            
        var currentStoreId = GetCurrentStoreId();
        if (!blogPost.AccessToEntityByStore(currentStoreId))
            return ErrorForKendoGridJson("Access denied");
            
        if (ModelState.IsValid)
        {
            await _blogViewModelService.UpdateProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        // First get the blog product to find the associated blog post
        var blogProduct = await _blogService.GetBlogProductById(id);
        if (blogProduct != null)
        {
            var blogPost = await _blogService.GetBlogPostById(blogProduct.BlogPostId);
            if (blogPost != null)
            {
                // Store limitation - check if blog post belongs to current store
                var currentStoreId = GetCurrentStoreId();
                if (!blogPost.AccessToEntityByStore(currentStoreId))
                    return ErrorForKendoGridJson("Access denied");
            }
        }
        
        if (ModelState.IsValid)
        {
            await _blogViewModelService.DeleteProductModel(id);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}