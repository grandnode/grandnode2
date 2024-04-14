using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Discounts;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class CollectionViewModelService : ICollectionViewModelService
{
    #region Constructors

    public CollectionViewModelService(
        ICollectionService collectionService,
        IProductCollectionService productCollectionService,
        ICollectionLayoutService collectionLayoutService,
        IProductService productService,
        IStoreService storeService,
        ISlugService slugService,
        IPictureService pictureService,
        ITranslationService translationService,
        IDiscountService discountService,
        IVendorService vendorService,
        ILanguageService languageService,
        IWorkContext workContext,
        SeoSettings seoSettings)
    {
        _collectionLayoutService = collectionLayoutService;
        _collectionService = collectionService;
        _productCollectionService = productCollectionService;
        _productService = productService;
        _storeService = storeService;
        _slugService = slugService;
        _pictureService = pictureService;
        _translationService = translationService;
        _discountService = discountService;
        _vendorService = vendorService;
        _languageService = languageService;
        _workContext = workContext;
        _seoSettings = seoSettings;
    }

    #endregion

    public virtual void PrepareSortOptionsModel(CollectionModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.AvailableSortOptions = ProductSortingEnum.Position.ToSelectList().ToList();
        model.AvailableSortOptions.Insert(0, new SelectListItem { Text = "None", Value = "-1" });
    }

    public virtual async Task PrepareLayoutsModel(CollectionModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var layouts = await _collectionLayoutService.GetAllCollectionLayouts();
        foreach (var layout in layouts)
            model.AvailableCollectionLayouts.Add(new SelectListItem {
                Text = layout.Name,
                Value = layout.Id
            });
    }


    public virtual async Task PrepareDiscountModel(CollectionModel model, Collection collection, bool excludeProperties)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.AvailableDiscounts = (await _discountService
                .GetDiscountsQuery(DiscountType.AssignedToCollections, _workContext.CurrentCustomer.Id))
            .Select(d => d.ToModel())
            .ToList();

        if (!excludeProperties && collection != null) model.SelectedDiscountIds = collection.AppliedDiscounts.ToArray();
    }

    public virtual async Task<Collection> InsertCollectionModel(CollectionModel model)
    {
        var collection = model.ToEntity();
        //discounts
        var allDiscounts = await _discountService.GetDiscountsQuery(DiscountType.AssignedToCollections);
        foreach (var discount in allDiscounts)
            if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
                collection.AppliedDiscounts.Add(discount.Id);

        await _collectionService.InsertCollection(collection);
        //search engine name
        collection.Locales =
            await model.Locales.ToTranslationProperty(collection, x => x.Name, _seoSettings, _slugService,
                _languageService);
        model.SeName = await collection.ValidateSeName(model.SeName, collection.Name, true, _seoSettings, _slugService,
            _languageService);
        collection.SeName = model.SeName;
        await _collectionService.UpdateCollection(collection);

        await _slugService.SaveSlug(collection, model.SeName, "");

        //update picture seo file name
        await _pictureService.UpdatePictureSeoNames(collection.PictureId, collection.Name);

        return collection;
    }

    public virtual async Task<Collection> UpdateCollectionModel(Collection collection, CollectionModel model)
    {
        var prevPictureId = collection.PictureId;
        collection = model.ToEntity(collection);
        collection.Locales =
            await model.Locales.ToTranslationProperty(collection, x => x.Name, _seoSettings, _slugService,
                _languageService);
        //discounts
        var allDiscounts = await _discountService.GetDiscountsQuery(DiscountType.AssignedToCollections);
        foreach (var discount in allDiscounts)
            if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
            {
                //new discount
                if (collection.AppliedDiscounts.Count(d => d == discount.Id) == 0)
                    collection.AppliedDiscounts.Add(discount.Id);
            }
            else
            {
                //remove discount
                if (collection.AppliedDiscounts.Count(d => d == discount.Id) > 0)
                    collection.AppliedDiscounts.Remove(discount.Id);
            }

        model.SeName = await collection.ValidateSeName(model.SeName, collection.Name, true, _seoSettings, _slugService,
            _languageService);
        collection.SeName = model.SeName;

        await _collectionService.UpdateCollection(collection);
        //search engine name
        await _slugService.SaveSlug(collection, model.SeName, "");

        //delete an old picture (if deleted or updated)
        if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != collection.PictureId)
        {
            var prevPicture = await _pictureService.GetPictureById(prevPictureId);
            if (prevPicture != null)
                await _pictureService.DeletePicture(prevPicture);
        }

        //update picture seo file name
        await _pictureService.UpdatePictureSeoNames(collection.PictureId, collection.Name);

        return collection;
    }

    public virtual async Task DeleteCollection(Collection collection)
    {
        await _collectionService.DeleteCollection(collection);
    }

    public virtual async Task<CollectionModel.AddCollectionProductModel> PrepareAddCollectionProductModel(
        string storeId)
    {
        var model = new CollectionModel.AddCollectionProductModel();

        //collections
        model.AvailableCollections.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var m in await _collectionService.GetAllCollections(showHidden: true))
            model.AvailableCollections.Add(new SelectListItem { Text = m.Name, Value = m.Id });

        //stores
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
        model.AvailableProductTypes =
            ProductType.SimpleProduct.ToSelectList(_translationService, _workContext, false).ToList();
        model.AvailableProductTypes.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "0" });
        return model;
    }

    public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(
        CollectionModel.AddCollectionProductModel model, int pageIndex, int pageSize)
    {
        var products = await _productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId,
            model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId,
            model.SearchProductName, pageIndex, pageSize);
        return (products.Select(x => x.ToModel()).ToList(), products.TotalCount);
    }

    public virtual async
        Task<(IEnumerable<CollectionModel.CollectionProductModel> collectionProductModels, int totalCount)>
        PrepareCollectionProductModel(string collectionId, string storeId, int pageIndex, int pageSize)
    {
        var productCollections = await _productCollectionService.GetProductCollectionsByCollectionId(collectionId,
            storeId,
            pageIndex - 1, pageSize, true);
        var items = new List<CollectionModel.CollectionProductModel>();
        foreach (var x in productCollections)
            items.Add(new CollectionModel.CollectionProductModel {
                Id = x.Id,
                CollectionId = x.CollectionId,
                ProductId = x.ProductId,
                ProductName = (await _productService.GetProductById(x.ProductId)).Name,
                IsFeaturedProduct = x.IsFeaturedProduct,
                DisplayOrder = x.DisplayOrder
            });
        return (items, productCollections.TotalCount);
    }

    public virtual async Task ProductUpdate(CollectionModel.CollectionProductModel model)
    {
        var product = await _productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var productCollection = product.ProductCollections.FirstOrDefault(x => x.Id == model.Id);
        if (productCollection == null)
            throw new ArgumentException("No product collection mapping found with the specified id");

        productCollection.IsFeaturedProduct = model.IsFeaturedProduct;
        productCollection.DisplayOrder = model.DisplayOrder;

        await _productCollectionService.UpdateProductCollection(productCollection, model.ProductId);
    }

    public virtual async Task ProductDelete(string id, string productId)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var productCollection = product.ProductCollections.FirstOrDefault(x => x.Id == id);
        if (productCollection == null)
            throw new ArgumentException("No product collection mapping found with the specified id");


        await _productCollectionService.DeleteProductCollection(productCollection, product.Id);
    }

    public virtual async Task InsertCollectionProductModel(CollectionModel.AddCollectionProductModel model)
    {
        foreach (var id in model.SelectedProductIds)
        {
            var product = await _productService.GetProductById(id);
            if (product != null)
                if (!product.ProductCollections.Any(x => x.CollectionId == model.CollectionId))
                    await _productCollectionService.InsertProductCollection(
                        new ProductCollection {
                            CollectionId = model.CollectionId,
                            IsFeaturedProduct = false,
                            DisplayOrder = 1
                        }, product.Id);
        }
    }

    #region Fields

    private readonly ICollectionService _collectionService;
    private readonly IProductCollectionService _productCollectionService;
    private readonly ICollectionLayoutService _collectionLayoutService;
    private readonly IProductService _productService;
    private readonly IStoreService _storeService;
    private readonly ISlugService _slugService;
    private readonly IPictureService _pictureService;
    private readonly ITranslationService _translationService;
    private readonly IDiscountService _discountService;
    private readonly IVendorService _vendorService;
    private readonly ILanguageService _languageService;
    private readonly IWorkContext _workContext;
    private readonly SeoSettings _seoSettings;

    #endregion
}