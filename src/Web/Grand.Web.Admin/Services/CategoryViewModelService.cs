using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Discounts;
using Grand.Domain.Seo;
using Grand.Web.Common.Extensions;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class CategoryViewModelService : ICategoryViewModelService
    {
        private readonly ICategoryService _categoryService;
        private readonly IProductCategoryService _productCategoryService;
        private readonly ICategoryLayoutService _categoryLayoutService;
        private readonly IDiscountService _discountService;
        private readonly ITranslationService _translationService;
        private readonly IStoreService _storeService;
        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService;
        private readonly ISlugService _slugService;
        private readonly IPictureService _pictureService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IProductService _productService;
        private readonly IVendorService _vendorService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILanguageService _languageService;
        private readonly CatalogSettings _catalogSettings;
        private readonly SeoSettings _seoSettings;

        public CategoryViewModelService(ICategoryService categoryService, IProductCategoryService productCategoryService, ICategoryLayoutService categoryLayoutService, IDiscountService discountService,
            ITranslationService translationService, IStoreService storeService, ICustomerService customerService, IGroupService groupService, IPictureService pictureService,
            ISlugService slugService, ICustomerActivityService customerActivityService, IProductService productService, 
            IVendorService vendorService, IDateTimeService dateTimeService, ILanguageService languageService, CatalogSettings catalogSettings, SeoSettings seoSettings)
        {
            _categoryService = categoryService;
            _productCategoryService = productCategoryService;
            _categoryLayoutService = categoryLayoutService;
            _discountService = discountService;
            _translationService = translationService;
            _storeService = storeService;
            _customerService = customerService;
            _groupService = groupService;
            _slugService = slugService;
            _customerActivityService = customerActivityService;
            _productService = productService;
            _pictureService = pictureService;
            _vendorService = vendorService;
            _languageService = languageService;
            _catalogSettings = catalogSettings;
            _dateTimeService = dateTimeService;
            _seoSettings = seoSettings;
        }

        protected virtual async Task PrepareLayoutsModel(CategoryModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var layouts = await _categoryLayoutService.GetAllCategoryLayouts();
            foreach (var layout in layouts)
            {
                model.AvailableCategoryLayouts.Add(new SelectListItem
                {
                    Text = layout.Name,
                    Value = layout.Id
                });
            }
        }

        protected virtual async Task PrepareDiscountModel(CategoryModel model, Category category, bool excludeProperties, string storeId)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.AvailableDiscounts = (await _discountService
                .GetAllDiscounts(DiscountType.AssignedToCategories, storeId: storeId, showHidden: true))
                .Select(d => d.ToModel(_dateTimeService))
                .ToList();

            if (!excludeProperties && category != null)
            {
                model.SelectedDiscountIds = category.AppliedDiscounts.ToArray();
            }
        }
        protected virtual void PrepareSortOptionsModel(CategoryModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.AvailableSortOptions = ProductSortingEnum.Position.ToSelectList().ToList();
            model.AvailableSortOptions.Insert(0, new SelectListItem { Text = "None", Value = "-1" });
        }

        public virtual async Task<CategoryListModel> PrepareCategoryListModel(string storeId)
        {
            var model = new CategoryListModel();
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
            foreach (var s in (await _storeService.GetAllStores()).Where(x => x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });
            return model;
        }

        public virtual async Task<(IEnumerable<CategoryModel> categoryListModel, int totalCount)> PrepareCategoryListModel(CategoryListModel model, int pageIndex, int pageSize)
        {
            var categories = await _categoryService.GetAllCategories(
                categoryName: model.SearchCategoryName, 
                storeId: model.SearchStoreId,
                pageSize: pageSize,
                pageIndex: pageIndex - 1, 
                showHidden: true);

            var categoryListModel = new List<CategoryModel>();
            foreach (var x in categories)
            {
                var categoryModel = x.ToModel();
                categoryModel.Breadcrumb = await _categoryService.GetFormattedBreadCrumb(x);
                categoryListModel.Add(categoryModel);
            }
            return (categoryListModel, categories.TotalCount);
        }

        public virtual async Task<CategoryModel> PrepareCategoryModel(string storeId)
        {
            var model = new CategoryModel();
            //sort options
            PrepareSortOptionsModel(model);
            //layouts
            await PrepareLayoutsModel(model);
            //discounts
            await PrepareDiscountModel(model, null, true, storeId);

            //default values
            model.PageSize = _catalogSettings.DefaultCategoryPageSize;
            model.PageSizeOptions = _catalogSettings.DefaultCategoryPageSizeOptions;
            model.Published = true;
            model.IncludeInMenu = true;
            model.AllowCustomersToSelectPageSize = true;
            return model;
        }

        public virtual async Task<CategoryModel> PrepareCategoryModel(CategoryModel model, Category category, string storeId)
        {
            //sort options
            PrepareSortOptionsModel(model);
            //layouts
            await PrepareLayoutsModel(model);
            //discounts
            await PrepareDiscountModel(model, category, false, storeId);
            return model;
        }

        public async Task<Category> InsertCategoryModel(CategoryModel model)
        {
            var category = model.ToEntity();
            category.CreatedOnUtc = DateTime.UtcNow;
            category.UpdatedOnUtc = DateTime.UtcNow;
            var allDiscounts = await _discountService.GetAllDiscounts(DiscountType.AssignedToCategories, showHidden: true);
            foreach (var discount in allDiscounts)
            {
                if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
                    category.AppliedDiscounts.Add(discount.Id);
            }
            await _categoryService.InsertCategory(category);

            //locales
            category.Locales = await model.Locales.ToTranslationProperty(category, x => x.Name, _seoSettings, _slugService, _languageService);
            model.SeName = await category.ValidateSeName(model.SeName, category.Name, true, _seoSettings, _slugService, _languageService);
            category.SeName = model.SeName;
            await _categoryService.UpdateCategory(category);

            await _slugService.SaveSlug(category, model.SeName, "");

            //update picture seo file name
            await _pictureService.UpdatePictureSeoNames(category.PictureId, category.Name);

            //activity log
            await _customerActivityService.InsertActivity("AddNewCategory", category.Id, _translationService.GetResource("ActivityLog.AddNewCategory"), category.Name);

            return category;
        }

        public virtual async Task<Category> UpdateCategoryModel(Category category, CategoryModel model)
        {
            string prevPictureId = category.PictureId;
            category = model.ToEntity(category);
            category.UpdatedOnUtc = DateTime.UtcNow;
            model.SeName = await category.ValidateSeName(model.SeName, category.Name, true, _seoSettings, _slugService, _languageService);
            category.SeName = model.SeName;
            //locales
            category.Locales = await model.Locales.ToTranslationProperty(category, x => x.Name, _seoSettings, _slugService, _languageService);
            await _categoryService.UpdateCategory(category);
            //search engine name
            await _slugService.SaveSlug(category, model.SeName, "");

            //discounts
            var allDiscounts = await _discountService.GetAllDiscounts(DiscountType.AssignedToCategories, showHidden: true);
            foreach (var discount in allDiscounts)
            {
                if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
                {
                    //new discount
                    if (category.AppliedDiscounts.Count(d => d == discount.Id) == 0)
                        category.AppliedDiscounts.Add(discount.Id);
                }
                else
                {
                    //remove discount
                    if (category.AppliedDiscounts.Count(d => d == discount.Id) > 0)
                        category.AppliedDiscounts.Remove(discount.Id);
                }
            }
            await _categoryService.UpdateCategory(category);
            //delete an old picture (if deleted or updated)
            if (!String.IsNullOrEmpty(prevPictureId) && prevPictureId != category.PictureId)
            {
                var prevPicture = await _pictureService.GetPictureById(prevPictureId);
                if (prevPicture != null)
                    await _pictureService.DeletePicture(prevPicture);
            }
            //update picture seo file name
            await _pictureService.UpdatePictureSeoNames(category.PictureId, category.Name);

            //activity log
            await _customerActivityService.InsertActivity("EditCategory", category.Id, _translationService.GetResource("ActivityLog.EditCategory"), category.Name);
            return category;
        }
        public virtual async Task DeleteCategory(Category category)
        {
            await _categoryService.DeleteCategory(category);
            //activity log
            await _customerActivityService.InsertActivity("DeleteCategory", category.Id, _translationService.GetResource("ActivityLog.DeleteCategory"), category.Name);
        }
        public virtual async Task<(IEnumerable<CategoryModel.CategoryProductModel> categoryProductModels, int totalCount)> PrepareCategoryProductModel(string categoryId, int pageIndex, int pageSize)
        {
            var productCategories = await _productCategoryService.GetProductCategoriesByCategoryId(categoryId,
                pageIndex - 1, pageSize, true);

            var categoryproducts = new List<CategoryModel.CategoryProductModel>();
            foreach (var item in productCategories)
            {
                var pc = new CategoryModel.CategoryProductModel
                {
                    Id = item.Id,
                    CategoryId = item.CategoryId,
                    ProductId = item.ProductId,
                    ProductName = (await _productService.GetProductById(item.ProductId))?.Name,
                    IsFeaturedProduct = item.IsFeaturedProduct,
                    DisplayOrder = item.DisplayOrder
                };
                categoryproducts.Add(pc);
            }
            return (categoryproducts, productCategories.TotalCount);
        }

        public virtual async Task<ProductCategory> UpdateProductCategoryModel(CategoryModel.CategoryProductModel model)
        {
            var product = await _productService.GetProductById(model.ProductId);
            var productCategory = product.ProductCategories.FirstOrDefault(x => x.Id == model.Id);
            if (productCategory == null)
                throw new ArgumentException("No product category mapping found with the specified id");

            productCategory.IsFeaturedProduct = model.IsFeaturedProduct;
            productCategory.DisplayOrder = model.DisplayOrder;
            await _productCategoryService.UpdateProductCategory(productCategory, product.Id);
            return productCategory;
        }
        public virtual async Task DeleteProductCategoryModel(string id, string productId)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null)
                throw new ArgumentException("No product found with the specified id");

            var productCategory = product.ProductCategories.Where(x => x.Id == id).FirstOrDefault();
            if (productCategory == null)
                throw new ArgumentException("No product category mapping found with the specified id");
            await _productCategoryService.DeleteProductCategory(productCategory, product.Id);

        }
        public virtual async Task<CategoryModel.AddCategoryProductModel> PrepareAddCategoryProductModel(string storeId)
        {
            var model = new CategoryModel.AddCategoryProductModel();
            //stores
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var s in (await _storeService.GetAllStores()).Where(x => x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            //vendors
            model.AvailableVendors.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var v in await _vendorService.GetAllVendors(showHidden: true))
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString() });

            //product types
            model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList().ToList();
            model.AvailableProductTypes.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "0" });
            return model;
        }

        public virtual async Task InsertCategoryProductModel(CategoryModel.AddCategoryProductModel model)
        {
            foreach (string id in model.SelectedProductIds)
            {
                var product = await _productService.GetProductById(id);
                if (product != null)
                {
                    if (product.ProductCategories.Where(x => x.CategoryId == model.CategoryId).Count() == 0)
                    {
                        await _productCategoryService.InsertProductCategory(
                            new ProductCategory
                            {
                                CategoryId = model.CategoryId,
                                IsFeaturedProduct = false,
                                DisplayOrder = 1,
                            }, product.Id);
                    }
                }
            }
        }
        public virtual async Task<(IEnumerable<CategoryModel.ActivityLogModel> activityLogModel, int totalCount)> PrepareActivityLogModel(string categoryId, int pageIndex, int pageSize)
        {
            var activityLog = await _customerActivityService.GetCategoryActivities(null, null, categoryId, pageIndex - 1, pageSize);
            var activityLogModelList = new List<CategoryModel.ActivityLogModel>();
            foreach (var item in activityLog)
            {
                var customer = await _customerService.GetCustomerById(item.CustomerId);
                var m = new CategoryModel.ActivityLogModel
                {
                    Id = item.Id,
                    ActivityLogTypeName = (await _customerActivityService.GetActivityTypeById(item.ActivityLogTypeId))?.Name,
                    Comment = item.Comment,
                    CreatedOn = _dateTimeService.ConvertToUserTime(item.CreatedOnUtc, DateTimeKind.Utc),
                    CustomerId = item.CustomerId,
                    CustomerEmail = customer != null ? customer.Email : "null"
                };
                activityLogModelList.Add(m);
            }
            return (activityLogModelList, activityLog.TotalCount);
        }
        public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(CategoryModel.AddCategoryProductModel model, int pageIndex, int pageSize)
        {
            var products = await _productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId, model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId, model.SearchProductName, pageIndex, pageSize);
            return (products.Select(x => x.ToModel(_dateTimeService)).ToList(), products.TotalCount);
        }
    }
}
