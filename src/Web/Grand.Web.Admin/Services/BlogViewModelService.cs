using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Blogs;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class BlogViewModelService : IBlogViewModelService
{
    private readonly IBlogService _blogService;
    private readonly ICustomerService _customerService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IPictureService _pictureService;
    private readonly IProductService _productService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IVendorService _vendorService;
    private readonly IContextAccessor _contextAccessor;
    private readonly ISeNameService _seNameService;
    private readonly IEnumTranslationService _enumTranslationService;
    
    public BlogViewModelService(
        IBlogService blogService, 
        IDateTimeService dateTimeService, 
        IStoreService storeService,
        IPictureService pictureService, 
        ICustomerService customerService, 
        ITranslationService translationService,
        IProductService productService,
        IVendorService vendorService, 
        IContextAccessor contextAccessor,
        ISeNameService seNameService, 
        IEnumTranslationService enumTranslationService)
    {
        _blogService = blogService;
        _dateTimeService = dateTimeService;
        _storeService = storeService;
        _pictureService = pictureService;
        _customerService = customerService;
        _translationService = translationService;
        _productService = productService;
        _vendorService = vendorService;
        _contextAccessor = contextAccessor;
        _seNameService = seNameService;
        _enumTranslationService = enumTranslationService;
    }

    public virtual async Task<(IEnumerable<BlogPostModel> blogPosts, int totalCount)> PrepareBlogPostsModel(
        int pageIndex, int pageSize)
    {
        var blogPosts = await _blogService.GetAllBlogPosts(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId, null, null,
            pageIndex - 1, pageSize, true);
        return (blogPosts.Select(x =>
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
        }), blogPosts.TotalCount);
    }

    public virtual async Task<BlogPost> InsertBlogPostModel(BlogPostModel model)
    {
        var blogPost = model.ToEntity(_dateTimeService);

        //search engine name
        blogPost.Locales = await _seNameService.TranslationSeNameProperties(model.Locales, blogPost, x => x.Title);
        blogPost.SeName = await _seNameService.ValidateSeName(blogPost, model.SeName, blogPost.Title, true);
        
        await _blogService.InsertBlogPost(blogPost);
        await _seNameService.SaveSeName(blogPost);

        //update picture seo file name
        await _pictureService.UpdatePictureSeoNames(blogPost.PictureId, blogPost.Title);

        return blogPost;
    }

    public virtual async Task<BlogPost> UpdateBlogPostModel(BlogPostModel model, BlogPost blogPost)
    {
        var prevPictureId = blogPost.PictureId;
        blogPost = model.ToEntity(blogPost, _dateTimeService);

        //search engine name
        blogPost.Locales = await _seNameService.TranslationSeNameProperties(model.Locales, blogPost, x => x.Title);
        blogPost.SeName = await _seNameService.ValidateSeName(blogPost, model.SeName, blogPost.Title, true);
        
        await _blogService.UpdateBlogPost(blogPost);
        await _seNameService.SaveSeName(blogPost);

        //delete an old picture (if deleted or updated)
        if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != blogPost.PictureId)
        {
            var prevPicture = await _pictureService.GetPictureById(prevPictureId);
            if (prevPicture != null)
                await _pictureService.DeletePicture(prevPicture);
        }

        //update picture seo file name
        await _pictureService.UpdatePictureSeoNames(blogPost.PictureId, blogPost.Title);

        return blogPost;
    }

    public virtual async Task<(IEnumerable<BlogCommentModel> blogComments, int totalCount)>
        PrepareBlogPostCommentsModel(string filterByBlogPostId, int pageIndex, int pageSize)
    {
        IList<BlogComment> comments;
        var storeId = string.IsNullOrEmpty(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            ? ""
            : _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        if (!string.IsNullOrEmpty(filterByBlogPostId))
        {
            //filter comments by blog
            var blogPost = await _blogService.GetBlogPostById(filterByBlogPostId);
            comments = await _blogService.GetBlogCommentsByBlogPostId(blogPost.Id);
        }
        else
        {
            //load all blog comments
            comments = await _blogService.GetAllComments("", storeId);
        }

        var commentsList = new List<BlogCommentModel>();
        foreach (var blogComment in comments.Skip((pageIndex - 1) * pageSize).Take(pageSize))
        {
            var commentModel = new BlogCommentModel {
                Id = blogComment.Id,
                BlogPostId = blogComment.BlogPostId,
                BlogPostTitle = blogComment.BlogPostTitle,
                CustomerId = blogComment.CustomerId
            };
            var customer = await _customerService.GetCustomerById(blogComment.CustomerId);
            commentModel.CustomerInfo = !string.IsNullOrEmpty(customer.Email)
                ? customer.Email
                : _translationService.GetResource("Admin.Customers.Guest");
            commentModel.CreatedOn = _dateTimeService.ConvertToUserTime(blogComment.CreatedOnUtc, DateTimeKind.Utc);
            commentModel.Comment = FormatText.ConvertText(blogComment.CommentText);
            commentsList.Add(commentModel);
        }

        return (commentsList, comments.Count);
    }

    public virtual async Task<(IEnumerable<BlogProductModel> blogProducts, int totalCount)> PrepareBlogProductsModel(
        string filterByBlogPostId, int pageIndex, int pageSize)
    {
        var productModels = new List<BlogProductModel>();
        var blogproducts = await _blogService.GetProductsByBlogPostId(filterByBlogPostId);
        foreach (var item in blogproducts.Skip((pageIndex - 1) * pageSize).Take(pageSize))
            productModels.Add(new BlogProductModel {
                Id = item.Id,
                DisplayOrder = item.DisplayOrder,
                ProductId = item.ProductId,
                ProductName = (await _productService.GetProductById(item.ProductId))?.Name
            });
        return (productModels, blogproducts.Count);
    }

    public virtual async Task<BlogProductModel.AddProductModel> PrepareBlogModelAddProductModel(string blogPostId)
    {
        var model = new BlogProductModel.AddProductModel {
            BlogPostId = blogPostId
        };

        //stores
        var storeId = _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        model.AvailableStores.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var s in (await _storeService.GetAllStores()).Where(x =>
                     x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });

        //vendors
        model.AvailableVendors.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var v in await _vendorService.GetAllVendors(showHidden: true))
            model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id });

        //product types
        model.AvailableProductTypes = _enumTranslationService.ToSelectList(ProductType.SimpleProduct).ToList();
        model.AvailableProductTypes.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });

        return model;
    }

    public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(
        BlogProductModel.AddProductModel model, int pageIndex, int pageSize)
    {
        model.SearchStoreId = _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        var products = await _productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId,
            model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId,
            model.SearchProductName, pageIndex, pageSize);
        return (products.Select(x => x.ToModel()).ToList(), products.TotalCount);
    }

    public virtual async Task InsertProductModel(string blogPostId, BlogProductModel.AddProductModel model)
    {
        foreach (var id in model.SelectedProductIds)
        {
            var products = await _blogService.GetProductsByBlogPostId(blogPostId);
            var product = await _productService.GetProductById(id);
            if (product != null)
                if (products.FirstOrDefault(x => x.ProductId == id) == null)
                    await _blogService.InsertBlogProduct(new BlogProduct {
                        BlogPostId = blogPostId,
                        ProductId = id,
                        DisplayOrder = 0
                    });
        }
    }

    public virtual async Task UpdateProductModel(BlogProductModel model)
    {
        var bp = await _blogService.GetBlogProductById(model.Id);
        bp.DisplayOrder = model.DisplayOrder;
        await _blogService.UpdateBlogProduct(bp);
    }

    public virtual async Task DeleteProductModel(string id)
    {
        var bp = await _blogService.GetBlogProductById(id);
        await _blogService.DeleteBlogProduct(bp);
    }
}