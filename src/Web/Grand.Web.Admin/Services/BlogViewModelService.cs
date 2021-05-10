using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Infrastructure;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Seo;
using Grand.Web.Common.Extensions;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Blogs;
using Grand.Web.Admin.Models.Catalog;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class BlogViewModelService : IBlogViewModelService
    {
        private readonly IBlogService _blogService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IStoreService _storeService;
        private readonly ISlugService _slugService;
        private readonly IPictureService _pictureService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly IVendorService _vendorService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        private readonly IWorkContext _workContext;
        private readonly SeoSettings _seoSettings;

        public BlogViewModelService(IBlogService blogService, IDateTimeService dateTimeService, IStoreService storeService, ISlugService slugService,
            IPictureService pictureService, ICustomerService customerService, ITranslationService translationService, IProductService productService,
            IVendorService vendorService, ILanguageService languageService, IWorkContext workContext, SeoSettings seoSettings)
        {
            _blogService = blogService;
            _dateTimeService = dateTimeService;
            _storeService = storeService;
            _slugService = slugService;
            _pictureService = pictureService;
            _customerService = customerService;
            _translationService = translationService;
            _productService = productService;
            _vendorService = vendorService;
            _languageService = languageService;
            _workContext = workContext;
            _seoSettings = seoSettings;
        }

        public virtual async Task<(IEnumerable<BlogPostModel> blogPosts, int totalCount)> PrepareBlogPostsModel(int pageIndex, int pageSize)
        {
            var blogPosts = await _blogService.GetAllBlogPosts(_workContext.CurrentCustomer.StaffStoreId, null, null, pageIndex - 1, pageSize, true);
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
            blogPost.CreatedOnUtc = DateTime.UtcNow;
            await _blogService.InsertBlogPost(blogPost);

            //search engine name
            var seName = await blogPost.ValidateSeName(model.SeName, model.Title, true, _seoSettings, _slugService, _languageService);
            blogPost.SeName = seName;
            blogPost.Locales = await model.Locales.ToTranslationProperty(blogPost, x => x.Title, _seoSettings, _slugService, _languageService);
            await _blogService.UpdateBlogPost(blogPost);
            await _slugService.SaveSlug(blogPost, seName, "");

            //update picture seo file name
            await _pictureService.UpdatePictureSeoNames(blogPost.PictureId, blogPost.Title);

            return blogPost;
        }

        public virtual async Task<BlogPost> UpdateBlogPostModel(BlogPostModel model, BlogPost blogPost)
        {
            string prevPictureId = blogPost.PictureId;
            blogPost = model.ToEntity(blogPost, _dateTimeService);
            await _blogService.UpdateBlogPost(blogPost);

            //search engine name
            var seName = await blogPost.ValidateSeName(model.SeName, model.Title, true, _seoSettings, _slugService, _languageService);
            blogPost.SeName = seName;
            blogPost.Locales = await model.Locales.ToTranslationProperty(blogPost, x => x.Title, _seoSettings, _slugService, _languageService);
            await _blogService.UpdateBlogPost(blogPost);
            await _slugService.SaveSlug(blogPost, seName, "");

            //delete an old picture (if deleted or updated)
            if (!String.IsNullOrEmpty(prevPictureId) && prevPictureId != blogPost.PictureId)
            {
                var prevPicture = await _pictureService.GetPictureById(prevPictureId);
                if (prevPicture != null)
                    await _pictureService.DeletePicture(prevPicture);
            }

            //update picture seo file name
            await _pictureService.UpdatePictureSeoNames(blogPost.PictureId, blogPost.Title);

            return blogPost;
        }
        public virtual async Task<(IEnumerable<BlogCommentModel> blogComments, int totalCount)> PrepareBlogPostCommentsModel(string filterByBlogPostId, int pageIndex, int pageSize)
        {
            IList<BlogComment> comments;
            var storeId = string.IsNullOrEmpty(_workContext.CurrentCustomer.StaffStoreId) ? "" : _workContext.CurrentCustomer.StaffStoreId;
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
                var commentModel = new BlogCommentModel
                {
                    Id = blogComment.Id,
                    BlogPostId = blogComment.BlogPostId,
                    BlogPostTitle = blogComment.BlogPostTitle,
                    CustomerId = blogComment.CustomerId
                };
                var customer = await _customerService.GetCustomerById(blogComment.CustomerId);
                commentModel.CustomerInfo = !string.IsNullOrEmpty(customer.Email) ? customer.Email : _translationService.GetResource("Admin.Customers.Guest");
                commentModel.CreatedOn = _dateTimeService.ConvertToUserTime(blogComment.CreatedOnUtc, DateTimeKind.Utc);
                commentModel.Comment = FormatText.ConvertText(blogComment.CommentText);
                commentsList.Add(commentModel);
            }
            return (commentsList, comments.Count);
        }

        public virtual async Task<(IEnumerable<BlogProductModel> blogProducts, int totalCount)> PrepareBlogProductsModel(string filterByBlogPostId, int pageIndex, int pageSize)
        {
            var productModels = new List<BlogProductModel>();
            var blogproducts = await _blogService.GetProductsByBlogPostId(filterByBlogPostId);
            foreach (var item in blogproducts.Skip((pageIndex - 1) * pageSize).Take(pageSize))
            {
                productModels.Add(new BlogProductModel()
                {
                    Id = item.Id,
                    DisplayOrder = item.DisplayOrder,
                    ProductId = item.ProductId,
                    ProductName = (await _productService.GetProductById(item.ProductId))?.Name
                });
            }
            return (productModels, blogproducts.Count);
        }

        public virtual async Task<BlogProductModel.AddProductModel> PrepareBlogModelAddProductModel(string blogPostId)
        {
            var model = new BlogProductModel.AddProductModel();
            model.BlogPostId = blogPostId;

            //stores
            var storeId = _workContext.CurrentCustomer.StaffStoreId;
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var s in (await _storeService.GetAllStores()).Where(x => x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            //vendors
            model.AvailableVendors.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var v in await _vendorService.GetAllVendors(showHidden: true))
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString() });

            //product types
            model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList().ToList();
            model.AvailableProductTypes.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });

            return model;
        }

        public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(BlogProductModel.AddProductModel model, int pageIndex, int pageSize)
        {
            model.SearchStoreId = _workContext.CurrentCustomer.StaffStoreId;
            var products = await _productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId, model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId, model.SearchProductName, pageIndex, pageSize);
            return (products.Select(x => x.ToModel(_dateTimeService)).ToList(), products.TotalCount);
        }

        public virtual async Task InsertProductModel(string blogPostId, BlogProductModel.AddProductModel model)
        {
            foreach (var id in model.SelectedProductIds)
            {
                var products = await _blogService.GetProductsByBlogPostId(blogPostId);
                var product = await _productService.GetProductById(id);
                if (product != null)
                {
                    if (products.FirstOrDefault(x => x.ProductId == id) == null)
                    {
                        await _blogService.InsertBlogProduct(new BlogProduct()
                        {
                            BlogPostId = blogPostId,
                            ProductId = id,
                            DisplayOrder = 0
                        });
                    }
                }
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
}
