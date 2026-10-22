using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.Seo;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;
using ProductExtensions = Grand.Domain.Catalog.ProductExtensions;

namespace Grand.Web.AdminShared.Services;

public class ProductViewModelService(
    IProductService productService,
    IInventoryManageService inventoryManageService,
    IPictureService pictureService,
    IProductAttributeService productAttributeService,
    IProductTagService productTagService,
    ICurrencyService currencyService,
    IMeasureService measureService,
    IDateTimeService dateTimeService,
    IBrandService brandService,
    ICollectionService collectionService,
    IProductCollectionService productCollectionService,
    ICategoryService categoryService,
    IProductCategoryService productCategoryService,
    IVendorService vendorService,
    IRequestTranslationService translationService,
    IProductLayoutService productLayoutService,
    ISpecificationAttributeService specificationAttributeService,
    IContextAccessor contextAccessor,
    IGroupService groupService,
    IWarehouseService warehouseService,
    IDeliveryDateService deliveryDateService,
    ITaxCategoryService taxCategoryService,
    IDiscountService discountService,
    ICustomerService customerService,
    IStoreService storeService,
    IOutOfStockSubscriptionService outOfStockSubscriptionService,
    IDownloadService downloadService,
    ILanguageService languageService,
    IProductAttributeFormatter productAttributeFormatter,
    IStockQuantityService stockQuantityService,
    CurrencySettings currencySettings,
    MeasureSettings measureSettings,
    TaxSettings taxSettings,
    SeoSettings seoSettings,
    IAuctionService auctionService,
    IPriceFormatter priceFormatter,
    ISeNameService seNameService,
    IEnumTranslationService enumTranslationService,
    IAdminDataScope<Product> scope)
    : IProductViewModelService
{
    public virtual async Task OutOfStockNotifications(Product product, int prevStockQuantity,
        List<ProductWarehouseInventory> prevMultiWarehouseStock
    )
    {
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock &&
            product.BackorderModeId == BackorderMode.NoBackorders &&
            product.AllowOutOfStockSubscriptions &&
            stockQuantityService.GetTotalStockQuantity(product, total: true) > 0 &&
            prevStockQuantity <= 0 && !product.UseMultipleWarehouses &&
            product.Published)
            await outOfStockSubscriptionService.SendNotificationsToSubscribers(product, "");
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock &&
            product.BackorderModeId == BackorderMode.NoBackorders &&
            product.AllowOutOfStockSubscriptions &&
            product.UseMultipleWarehouses &&
            product.Published)
        {
            foreach (var prevstock in prevMultiWarehouseStock)
                if (prevstock.StockQuantity - prevstock.ReservedQuantity <= 0)
                {
                    var actualStock =
                        product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == prevstock.WarehouseId);
                    if (actualStock != null)
                        if (actualStock.StockQuantity - actualStock.ReservedQuantity > 0)
                            await outOfStockSubscriptionService.SendNotificationsToSubscribers(product,
                                prevstock.WarehouseId);
                }

            if (product.ProductWarehouseInventory.Sum(x => x.StockQuantity - x.ReservedQuantity) > 0)
                if (prevMultiWarehouseStock.Sum(x => x.StockQuantity - x.ReservedQuantity) <= 0)
                    await outOfStockSubscriptionService.SendNotificationsToSubscribers(product, "");
        }
    }

    public virtual async Task PrepareProductModel(ProductModel model, Product product,
        bool setPredefinedValues, bool excludeProperties)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.PrimaryStoreCurrencyCode = (await currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId))?.CurrencyCode;
        model.BaseWeightIn = (await measureService.GetMeasureWeightById(measureSettings.BaseWeightId))?.Name;
        model.BaseDimensionIn = (await measureService.GetMeasureDimensionById(measureSettings.BaseDimensionId))?.Name;

        if (product != null)
        {
            //date
            model.CreatedOn = dateTimeService.ConvertToUserTime(product.CreatedOnUtc, DateTimeKind.Utc);
            model.UpdatedOn = product.UpdatedOnUtc.HasValue
                ? dateTimeService.ConvertToUserTime(product.UpdatedOnUtc.Value, DateTimeKind.Utc)
                : null;

            //parent grouped product
            var parentGroupedProduct = await productService.GetProductById(product.ParentGroupedProductId);
            if (parentGroupedProduct != null)
            {
                model.AssociatedToProductId = product.ParentGroupedProductId;
                model.AssociatedToProductName = parentGroupedProduct.Name;
            }

            //reservation
            model.CalendarModel.ProductId = product.Id;
            model.CalendarModel.Interval = product.Interval;
            model.CalendarModel.IntervalUnit = (int)product.IntervalUnitId;
            model.CalendarModel.IncBothDate = product.IncBothDate;

            model.AutoAddRequiredProducts = product.AutoAddRequiredProducts;
            //product attributes
            foreach (var productAttribute in await productAttributeService.GetAllProductAttributes(contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
                model.AvailableProductAttributes.Add(new SelectListItem {
                    Text = productAttribute.Name,
                    Value = productAttribute.Id
                });
        }

        //copy product
        if (product != null)
        {
            model.CopyProductModel.Id = product.Id;
            model.CopyProductModel.Name = "Copy of " + product.Name;
            model.CopyProductModel.Published = true;
            model.CopyProductModel.CopyImages = true;
        }

        //layouts
        var layouts = await productLayoutService.GetAllProductLayouts();
        foreach (var layout in layouts)
            model.AvailableProductLayouts.Add(new SelectListItem {
                Text = layout.Name,
                Value = layout.Id
            });

        //delivery dates
        model.AvailableDeliveryDates.Add(new SelectListItem {
            Text = translationService.GetResource("Admin.Catalog.Products.Fields.DeliveryDate.None"),
            Value = ""
        });
        var deliveryDates = await deliveryDateService.GetAllDeliveryDates(model.StoreId);
        foreach (var deliveryDate in deliveryDates)
            model.AvailableDeliveryDates.Add(new SelectListItem {
                Text = deliveryDate.Name,
                Value = deliveryDate.Id
            });

        //warehouses
        var warehouses = await warehouseService.GetAllWarehouses(model.StoreId);
        model.AvailableWarehouses.Add(new SelectListItem {
            Text = translationService.GetResource("Admin.Catalog.Products.Fields.Warehouse.None"),
            Value = ""
        });
        foreach (var warehouse in warehouses)
            model.AvailableWarehouses.Add(new SelectListItem {
                Text = warehouse.Name,
                Value = warehouse.Id
            });

        //multiple warehouses
        foreach (var warehouse in warehouses)
        {
            var pwiModel = new ProductModel.ProductWarehouseInventoryModel {
                WarehouseId = warehouse.Id,
                WarehouseName = warehouse.Name,
                WarehouseCode = warehouse.Code
            };
            if (product != null)
            {
                var pwi = product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouse.Id);
                if (pwi != null)
                {
                    pwiModel.WarehouseUsed = true;
                    pwiModel.StockQuantity = pwi.StockQuantity;
                    pwiModel.ReservedQuantity = pwi.ReservedQuantity;
                }
            }

            model.ProductWarehouseInventoryModels.Add(pwiModel);
        }

        //product tags
        if (product != null)
        {
            var result = new StringBuilder();
            for (var i = 0; i < product.ProductTags.Count; i++)
            {
                var pt = product.ProductTags.ToList()[i];
                var productTag = await productTagService.GetProductTagByName(pt);
                if (productTag != null)
                {
                    result.Append(productTag.Name);
                    if (i != product.ProductTags.Count - 1)
                        result.Append(", ");
                }
            }

            model.ProductTags = result.ToString();
        }

        //tax categories
        var taxCategories = await taxCategoryService.GetAllTaxCategories(contextAccessor.WorkContext.CurrentCustomer.StaffStoreId);
        model.AvailableTaxCategories.Add(new SelectListItem {
            Text = translationService.GetResource("Admin.Configuration.Tax.Settings.TaxCategories.None"),
            Value = ""
        });
        foreach (var tc in taxCategories)
            model.AvailableTaxCategories.Add(new SelectListItem {
                Text = tc.Name,
                Value = tc.Id,
                Selected = product != null && !setPredefinedValues && tc.Id == product.TaxCategoryId
            });

        //baseprice units
        var measureWeights = await measureService.GetAllMeasureWeights();
        foreach (var mw in measureWeights)
            model.AvailableBasepriceUnits.Add(new SelectListItem {
                Text = mw.Name,
                Value = mw.Id,
                Selected = product != null && !setPredefinedValues && mw.Id == product.BasepriceUnitId
            });
        foreach (var mw in measureWeights)
            model.AvailableBasepriceBaseUnits.Add(new SelectListItem {
                Text = mw.Name,
                Value = mw.Id,
                Selected = product != null && !setPredefinedValues && mw.Id == product.BasepriceBaseUnitId
            });

        //units
        var units = await measureService.GetAllMeasureUnits();
        model.AvailableUnits.Add(new SelectListItem { Text = "---", Value = "" });
        foreach (var un in units)
            model.AvailableUnits.Add(new SelectListItem { Text = un.Name, Value = un.Id, Selected = product != null && un.Id == product.UnitId });

        //discounts
        model.AvailableDiscounts = (await discountService.GetDiscountsQuery(DiscountType.AssignedToSkus, contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            .Select(d => d.ToModel(dateTimeService))
            .ToList();
        if (!excludeProperties && product != null) model.SelectedDiscountIds = product.AppliedDiscounts.ToArray();

        //default values
        if (setPredefinedValues)
        {
            model.MaxEnteredPrice = 1000;
            model.MaxNumberOfDownloads = 10;
            model.RecurringCycleLength = 100;
            model.RecurringTotalCycles = 10;
            model.StockQuantity = 0;
            model.NotifyAdminForQuantityBelow = 1;
            model.OrderMinimumQuantity = 1;
            model.OrderMaximumQuantity = 10000;
            model.TaxCategoryId = taxSettings.DefaultTaxCategoryId;
            model.UnlimitedDownloads = true;
            model.IsShipEnabled = true;
            model.AllowCustomerReviews = true;
            model.Published = true;
            model.VisibleIndividually = true;
        }
    }

    public virtual async Task SaveProductWarehouseInventory(Product product,
        IList<ProductModel.ProductWarehouseInventoryModel> model)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStock)
            return;

        if (!product.UseMultipleWarehouses)
            return;

        var warehouses = await warehouseService.GetAllWarehouses(product.Stores.Count == 1 ? product.Stores.First() : null);

        foreach (var warehouse in warehouses)
        {
            var whim = model.FirstOrDefault(x => x.WarehouseId == warehouse.Id);
            var existingPwI = product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouse.Id);
            if (existingPwI != null)
            {
                if (whim is { WarehouseUsed: true })
                {
                    //update existing record
                    existingPwI.StockQuantity = whim.StockQuantity;
                    existingPwI.ReservedQuantity = whim.ReservedQuantity;
                    await productService.UpdateProductWarehouseInventory(existingPwI, product.Id);
                }
                else
                {
                    //delete. no need to store record for qty 0
                    await productService.DeleteProductWarehouseInventory(existingPwI, product.Id);
                }
            }
            else
            {
                if (whim is { WarehouseUsed: true })
                {
                    //no need to insert a record for qty 0
                    existingPwI = new ProductWarehouseInventory {
                        WarehouseId = warehouse.Id,
                        StockQuantity = whim.StockQuantity,
                        ReservedQuantity = whim.ReservedQuantity
                    };
                    product.ProductWarehouseInventory.Add(existingPwI);
                    await productService.InsertProductWarehouseInventory(existingPwI, product.Id);
                }
            }
        }

        product.StockQuantity = product.ProductWarehouseInventory.Sum(x => x.StockQuantity);
        product.ReservedQuantity = product.ProductWarehouseInventory.Sum(x => x.ReservedQuantity);
        await inventoryManageService.UpdateStockProduct(product, false);
    }

    public virtual async Task PrepareProductReviewModel(ProductReviewModel model,
        ProductReview productReview, bool excludeProperties, bool formatReviewText)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(productReview);

        var product = await productService.GetProductById(productReview.ProductId);
        var customer = await customerService.GetCustomerById(productReview.CustomerId);
        var store = await storeService.GetStoreById(productReview.StoreId);
        model.Id = productReview.Id;
        model.StoreName = store != null ? store.Shortcut : "";
        model.ProductId = productReview.ProductId;
        model.ProductName = product.Name;
        model.CustomerId = productReview.CustomerId;
        model.CustomerInfo = customer != null
            ? !string.IsNullOrEmpty(customer.Email)
                ? customer.Email
                : translationService.GetResource("Admin.Customers.Guest")
            : "";
        model.Rating = productReview.Rating;
        model.CreatedOn = dateTimeService.ConvertToUserTime(productReview.CreatedOnUtc, DateTimeKind.Utc);
        model.Signature = productReview.Signature;
        if (!excludeProperties)
        {
            model.Title = productReview.Title;
            if (formatReviewText)
            {
                model.ReviewText = FormatText.ConvertText(productReview.ReviewText);
                model.ReplyText = FormatText.ConvertText(productReview.ReplyText);
            }
            else
            {
                model.ReviewText = productReview.ReviewText;
                model.ReplyText = productReview.ReplyText;
            }

            model.IsApproved = productReview.IsApproved;
        }
    }

    public virtual async Task<ProductListModel> PrepareProductListModel()
    {
        var model = new ProductListModel();
        var storeId = scope.DefaultStoreId ?? "";

        //stores - Vendor has no store concept at all (ShowStoreSelector is false), so the dropdown
        //is left empty entirely rather than populated off storeId, which is "" for Vendor same as Admin.
        if (scope.ShowStoreSelector)
        {
            model.AvailableStores.Add(new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Common.All"), Value = " " });
            foreach (var s in (await storeService.GetAllStores()).Where(x =>
                         x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });
        }

        //warehouses
        model.AvailableWarehouses.Add(new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Common.All"), Value = " " });
        foreach (var wh in await warehouseService.GetAllWarehouses(storeId))
            model.AvailableWarehouses.Add(new SelectListItem { Text = wh.Name, Value = wh.Id });

        //product types
        model.AvailableProductTypes = enumTranslationService.ToSelectList(ProductType.SimpleProduct, false).ToList();
        model.AvailableProductTypes.Insert(0,
            new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Common.All"), Value = "0" });

        //"published" property
        //0 - all (according to "ShowHidden" parameter)
        //1 - published only
        //2 - unpublished only
        //3 - Show on homepage
        //4 - mark as new
        model.AvailablePublishedOptions.Add(new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.List.SearchPublished.All"), Value = " " });
        model.AvailablePublishedOptions.Add(new SelectListItem {
            Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.List.SearchPublished.PublishedOnly"),
            Value = "1"
        });
        model.AvailablePublishedOptions.Add(new SelectListItem {
            Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.List.SearchPublished.UnpublishedOnly"),
            Value = "2"
        });

        // Admin/Store show "Show on homepage" (value 3); Vendor's original copy omits it entirely - vendors
        // can't feature products on the homepage, a real capability difference, not a naming difference.
        if (scope.CanFeatureOnHomepage)
            model.AvailablePublishedOptions.Add(new SelectListItem {
                Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.List.SearchPublished.ShowOnHomePage"),
                Value = "3"
            });

        model.AvailablePublishedOptions.Add(new SelectListItem {
            Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.List.SearchPublished.MarkAsNew"),
            Value = "4"
        });

        return model;
    }

    public virtual async Task<(IEnumerable<ProductModel> productModels, int totalCount)> PrepareProductsModel(
        ProductListModel model, int pageIndex, int pageSize)
    {

        var categoryIds = new List<string>();
        if (!string.IsNullOrEmpty(model.SearchCategoryId))
            categoryIds.Add(model.SearchCategoryId);

        //include subcategories
        if (model.SearchIncludeSubCategories && !string.IsNullOrEmpty(model.SearchCategoryId))
            categoryIds.AddRange(await GetChildCategoryIds(model.SearchCategoryId));

        //0 - all (according to "ShowHidden" parameter)
        //1 - published only
        //2 - unpublished only
        bool? overridePublished = null;
        switch (model.SearchPublishedId)
        {
            case 1:
                overridePublished = true;
                break;
            case 2:
                overridePublished = false;
                break;
        }

        bool? showOnHomePage = null;
        if (model.SearchPublishedId == 3)
            showOnHomePage = true;

        var markedAsNewOnly = model.SearchPublishedId == 4;

        // vendorId: scope.DefaultVendorId (when set) overrides whatever the search model asks for - mirrors
        // Vendor's original PrepareProductsModel, which always passed vendorId: CurrentVendor.Id regardless
        // of any vendor filter on the model. Admin/Store have no DefaultVendorId, so this falls back to
        // model.SearchVendorId unchanged for them.
        var products = (await productService.SearchProducts(
            access: CatalogAccess.IncludingHidden(model.SearchStoreId),
            categoryIds: categoryIds,
            brandId: model.SearchBrandId,
            collectionId: model.SearchCollectionId,
            vendorId: scope.DefaultVendorId ?? model.SearchVendorId,
            warehouseId: model.SearchWarehouseId,
            productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
            keywords: model.SearchProductName,
            pageIndex: pageIndex - 1,
            pageSize: pageSize,
            showOnHomePage: showOnHomePage,
            overridePublished: overridePublished,
            markedAsNewOnly: markedAsNewOnly
        )).products;

        var items = new List<ProductModel>();
        //the brand and the first category of every row on the page, each looked up once: both
        //services read through the cache, and a page is 15 rows of mostly repeating ids
        var brandNames = new Dictionary<string, string>();
        var categoryNames = new Dictionary<string, string>();
        foreach (var x in products)
        {
            var productModel = x.ToModel(dateTimeService);
            //"Error during serialization or deserialization using the JSON JavaScriptSerializer. The length of the string exceeds the value set on the maxJsonLength property. "
            //also it improves performance
            productModel.FullDescription = "";

            //picture
            var defaultProductPicture = x.ProductPictures.FirstOrDefault() ?? new ProductPicture();
            productModel.PictureThumbnailUrl = await pictureService.GetPictureUrl(defaultProductPicture.PictureId, 100);
            //product type
            productModel.ProductTypeName = enumTranslationService.GetTranslationEnum(x.ProductTypeId);
            //brand
            if (!string.IsNullOrEmpty(x.BrandId))
            {
                if (!brandNames.TryGetValue(x.BrandId, out var brandName))
                {
                    brandName = (await brandService.GetBrandById(x.BrandId))?.Name;
                    brandNames[x.BrandId] = brandName;
                }

                productModel.BrandName = brandName;
            }

            //the first category the product is mapped to
            var firstCategoryId = x.ProductCategories.FirstOrDefault()?.CategoryId;
            if (!string.IsNullOrEmpty(firstCategoryId))
            {
                if (!categoryNames.TryGetValue(firstCategoryId, out var categoryName))
                {
                    categoryName = (await categoryService.GetCategoryById(firstCategoryId))?.Name;
                    categoryNames[firstCategoryId] = categoryName;
                }

                productModel.CategoryName = categoryName;
            }
            //friendly stock quantity
            //if a simple product AND "manage inventory" is "Track inventory", then display
            if (x.ProductTypeId == ProductType.SimpleProduct &&
                x.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
                productModel.StockQuantityStr = stockQuantityService.GetTotalStockQuantity(x, total: true).ToString();
            items.Add(productModel);
        }

        return (items, products.TotalCount);
    }

    public virtual async Task<IList<Product>> PrepareProducts(ProductListModel model)
    {
        var categoryIds = new List<string>();
        if (!string.IsNullOrEmpty(model.SearchCategoryId))
            categoryIds.Add(model.SearchCategoryId);

        //include subcategories
        if (model.SearchIncludeSubCategories && !string.IsNullOrEmpty(model.SearchCategoryId))
            categoryIds.AddRange(await GetChildCategoryIds(model.SearchCategoryId));

        //0 - all (according to "ShowHidden" parameter)
        //1 - published only
        //2 - unpublished only
        bool? overridePublished = null;
        switch (model.SearchPublishedId)
        {
            case 1:
                overridePublished = true;
                break;
            case 2:
                overridePublished = false;
                break;
        }

        // vendorId: same override as PrepareProductsModel above - mirrors Vendor's original PrepareProducts,
        // which always passed vendorId: CurrentVendor.Id regardless of the search model.
        var products = (await productService.SearchProducts(
            access: CatalogAccess.IncludingHidden(model.SearchStoreId),
            categoryIds: categoryIds,
            brandId: model.SearchBrandId,
            collectionId: model.SearchCollectionId,
            vendorId: scope.DefaultVendorId ?? model.SearchVendorId,
            warehouseId: model.SearchWarehouseId,
            productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
            keywords: model.SearchProductName,
            overridePublished: overridePublished
        )).products;

        return products;
    }

    public virtual async Task<Product> InsertProductModel(ProductModel model)
    {
        //product
        var product = model.ToEntity(dateTimeService);

        // VendorId: forced onto new products when scope.DefaultVendorId is set - mirrors Vendor's original
        // InsertProductModel, which always set product.VendorId = CurrentVendor?.Id explicitly and ignored
        // VendorId in its AutoMapper profile for exactly this reason. AdminShared's shared ProductProfile
        // does *not* ignore ProductModel.VendorId -> Product.VendorId (Admin's create form legitimately
        // assigns vendor ownership), so without this, a vendor host would map whatever (usually blank)
        // VendorId the model carries, leaving newly-created products belonging to no vendor at all -
        // orphaned and inaccessible to the vendor who just created them once VendorProductDataScope.HasAccess
        // gates on VendorId equality.
        if (scope.DefaultVendorId is not null) product.VendorId = scope.DefaultVendorId;

        //discounts
        var allDiscounts = await discountService.GetDiscountsQuery(DiscountType.AssignedToSkus, model.StoreId);
        foreach (Discount discount in allDiscounts)
            if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
            {
                product.AppliedDiscounts.Add(discount.Id);
            }

        product.Locales = await seNameService.TranslationSeNameProperties(model.Locales, product, x => x.Name);
        product.SeName = await seNameService.ValidateSeName(product, model.SeName, product.Name, true);

        await productService.InsertProduct(product);

        //search engine name
        await seNameService.SaveSeName(product);

        //tags
        await SaveProductTags(product, ParseProductTags(model.ProductTags));

        //warehouses
        await SaveProductWarehouseInventory(product, model.ProductWarehouseInventoryModels);

        await productService.UpdateProduct(product);

        return product;
    }

    public virtual async Task<Product> UpdateProductModel(Product product, ProductModel model)
    {
        var prevStockQuantity = stockQuantityService.GetTotalStockQuantity(product, total: true);
        var prevMultiWarehouseStock = product.ProductWarehouseInventory.Select(i => new ProductWarehouseInventory { WarehouseId = i.WarehouseId, StockQuantity = i.StockQuantity, ReservedQuantity = i.ReservedQuantity })
            .ToList();

        var prevDownloadId = product.DownloadId;
        var prevSampleDownloadId = product.SampleDownloadId;

        //product
        product = model.ToEntity(product, dateTimeService);
        product.AutoAddRequiredProducts = model.AutoAddRequiredProducts;

        // VendorId: re-forced to scope.DefaultVendorId (when set) after mapping, not merely left alone -
        // AdminShared's shared ProductProfile does not ignore ProductModel.VendorId -> Product.VendorId, so
        // without this, mapping a caller-supplied model onto the existing entity would let a vendor
        // reassign their own product to a different vendor id via a tampered/mass-assigned VendorId field.
        // Vendor's original UpdateProductModel never touched VendorId at all (its own AutoMapper profile
        // ignores it), relying on the pre-loaded entity's existing value - forcing it here is strictly
        // equivalent for legitimately-owned products (already gated by scope.HasAccess before reaching this
        // method) while also closing that mass-assignment gap for Admin/Store's shared profile.
        if (scope.DefaultVendorId is not null) product.VendorId = scope.DefaultVendorId;
        product.Locales = await seNameService.TranslationSeNameProperties(model.Locales, product, x => x.Name);
        product.SeName = await seNameService.ValidateSeName(product, model.SeName, product.Name, true);
        //discounts
        var allDiscounts = await discountService.GetDiscountsQuery(DiscountType.AssignedToSkus, model.StoreId);

        foreach (var discount in allDiscounts)
            if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
            {
                //new discount
                if (product.AppliedDiscounts.Count(d => d == discount.Id) == 0)
                {
                    product.AppliedDiscounts.Add(discount.Id);
                }
            }
            else
            {
                //remove discount
                if (product.AppliedDiscounts.Count(d => d == discount.Id) > 0)
                {
                    product.AppliedDiscounts.Remove(discount.Id);
                }
            }

        await productService.UpdateProduct(product);

        //search engine name
        await seNameService.SaveSeName(product);
        //tags
        await SaveProductTags(product, ParseProductTags(model.ProductTags));
        //warehouses
        await SaveProductWarehouseInventory(product, model.ProductWarehouseInventoryModels);
        //picture seo names
        await UpdatePictureSeoNames(product);

        //out of stock notifications
        await OutOfStockNotifications(product, prevStockQuantity, prevMultiWarehouseStock);

        //delete an old "download" file (if deleted or updated)
        if (!string.IsNullOrEmpty(prevDownloadId) && prevDownloadId != product.DownloadId)
        {
            var prevDownload = await downloadService.GetDownloadById(prevDownloadId);
            if (prevDownload != null)
                await downloadService.DeleteDownload(prevDownload);
        }

        //delete an old "sample download" file (if deleted or updated)
        if (!string.IsNullOrEmpty(prevSampleDownloadId) && prevSampleDownloadId != product.SampleDownloadId)
        {
            var prevSampleDownload = await downloadService.GetDownloadById(prevSampleDownloadId);
            if (prevSampleDownload != null)
                await downloadService.DeleteDownload(prevSampleDownload);
        }

        return product;
    }

    public virtual async Task DeleteProduct(Product product)
    {
        await productService.DeleteProduct(product);

        //delete an "download" file
        if (!string.IsNullOrEmpty(product.DownloadId))
        {
            var download = await downloadService.GetDownloadById(product.DownloadId);
            if (download != null)
                await downloadService.DeleteDownload(download);
        }

        if (!string.IsNullOrEmpty(product.SampleDownloadId))
        {
            var sampledownload = await downloadService.GetDownloadById(product.SampleDownloadId);
            if (sampledownload != null)
                await downloadService.DeleteDownload(sampledownload);
        }
    }

    public virtual async Task DeleteSelected(IEnumerable<string> selectedIds)
    {
        var products = new List<Product>();
        products.AddRange(await productService.GetProductsByIds(selectedIds.ToArray(),
            access: CatalogAccess.IncludingHidden()));
        for (var i = 0; i < products.Count; i++)
        {
            await DeleteProduct(products[i]);
        }
    }

    public virtual async Task<ProductModel.AddRequiredProductModel> PrepareAddRequiredProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddRequiredProductModel>();
        return model;
    }

    public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(
        ProductModel.AddProductModel model, int pageIndex, int pageSize)
    {
        // vendorId: scope.DefaultVendorId (when set) overrides the search model - mirrors Vendor's original
        // PrepareProductModel(AddProductModel,...), which always passed vendorId: CurrentVendor.Id (and
        // storeId: string.Empty) regardless of the model, so the "add related/similar/bundle/etc. product"
        // popups can never surface another vendor's products.
        var products = await productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId,
            model.SearchCollectionId, model.SearchStoreId, scope.DefaultVendorId ?? model.SearchVendorId, model.SearchProductTypeId,
            model.SearchProductName, pageIndex, pageSize);
        return (products.Select(x => x.ToModel(dateTimeService)).ToList(), products.TotalCount);
    }

    public virtual async Task<ProductModel.AddRelatedProductModel> PrepareRelatedProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddRelatedProductModel>();
        return model;
    }

    public virtual async Task<ProductModel.AddSimilarProductModel> PrepareSimilarProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddSimilarProductModel>();
        return model;
    }

    public virtual async Task<ProductModel.AddBundleProductModel> PrepareBundleProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddBundleProductModel>();
        return model;
    }

    public virtual async Task<ProductModel.AddCrossSellProductModel> PrepareCrossSellProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddCrossSellProductModel>();
        return model;
    }

    public virtual async Task<ProductModel.AddRecommendedProductModel> PrepareRecommendedProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddRecommendedProductModel>();
        return model;
    }

    public virtual async Task<ProductModel.AddAssociatedProductModel> PrepareAssociatedProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddAssociatedProductModel>();
        return model;
    }

    public virtual async Task<BulkEditListModel> PrepareBulkEditListModel()
    {
        var storeId = scope.DefaultStoreId ?? "";
        var model = new BulkEditListModel();

        //product types
        model.AvailableProductTypes = enumTranslationService.ToSelectList(ProductType.SimpleProduct, false).ToList();
        model.AvailableProductTypes.Insert(0,
            new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = "0" });

        // avaible stores - same capability gate as PrepareProductListModel: Vendor's original never
        // populated a stores dropdown here at all (vendors don't pick stores), so this must not run for
        // Vendor even though its storeId also resolves to "".
        if (scope.ShowStoreSelector)
        {
            if (!string.IsNullOrEmpty(storeId))
            {
                var store = (await storeService.GetAllStores()).FirstOrDefault(x => x.Id == storeId);
                if (store != null)
                    model.AvailableStores.Add(new SelectListItem { Text = store.Shortcut, Value = store.Id });
            }
            else
            {
                model.AvailableStores.Add(new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = "" });

                foreach (var s in await storeService.GetAllStores())
                    model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });
            }
        }

        return model;
    }

    public virtual async Task<(IEnumerable<BulkEditProductModel> bulkEditProductModels, int totalCount)>
        PrepareBulkEditProductModel(BulkEditListModel model, int pageIndex, int pageSize)
    {
        var searchCategoryIds = new List<string>();
        if (!string.IsNullOrEmpty(model.SearchCategoryId))
            searchCategoryIds.Add(model.SearchCategoryId);

        // vendorId: scope.DefaultVendorId forces the bulk-edit grid to the current vendor's own products.
        // Vendor's original PrepareBulkEditProductModel always passed vendorId: CurrentVendor.Id; this
        // shared version previously had no vendor filtering at all - a real gap, since BulkEditListModel
        // has no SearchVendorId field for a caller to (mis)supply in the first place, so there is nothing
        // to fall back to for Admin/Store, whose scope.DefaultVendorId is null and who intentionally see
        // every vendor's products in bulk-edit.
        var products = (await productService.SearchProducts(
            access: CatalogAccess.IncludingHidden(model.SearchStoreId), categoryIds: searchCategoryIds,
            brandId: model.SearchBrandId,
            collectionId: model.SearchCollectionId,
            vendorId: scope.DefaultVendorId ?? "",
            productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
            keywords: model.SearchProductName,
            pageIndex: pageIndex - 1,
            pageSize: pageSize)).products;

        var items = new List<BulkEditProductModel>();
        foreach (var x in products)
        {
            var productModel = new BulkEditProductModel {
                Id = x.Id,
                Name = x.Name,
                Sku = x.Sku,
                OldPrice = x.OldPrice,
                Price = x.Price,
                ManageInventoryMethodId = (int)x.ManageInventoryMethodId,
                ManageInventoryMethod = enumTranslationService.GetTranslationEnum(x.ManageInventoryMethodId),
                StockQuantity = x.StockQuantity,
                Published = x.Published
            };
            items.Add(productModel);
        }

        return (items, products.TotalCount);
    }

    public virtual async Task UpdateBulkEdit(IEnumerable<BulkEditProductModel> products)
    {
        foreach (var pModel in products)
        {
            //update
            var product = await productService.GetProductById(pModel.Id, true);
            if (product != null)
            {
                var prevStockQuantity = stockQuantityService.GetTotalStockQuantity(product, total: true);

                product.Sku = pModel.Sku;
                product.Price = pModel.Price;
                product.OldPrice = pModel.OldPrice;
                product.StockQuantity = pModel.StockQuantity;
                product.Published = pModel.Published;
                product.Name = pModel.Name;
                product.ManageInventoryMethodId = (ManageInventoryMethod)pModel.ManageInventoryMethodId;
                await productService.UpdateProduct(product);

                //out of stock notifications
                if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock &&
                    product.BackorderModeId == BackorderMode.NoBackorders &&
                    product.AllowOutOfStockSubscriptions &&
                    stockQuantityService.GetTotalStockQuantity(product, total: true) > 0 &&
                    prevStockQuantity <= 0 && !product.UseMultipleWarehouses &&
                    product.Published)
                    await outOfStockSubscriptionService.SendNotificationsToSubscribers(product, "");
            }
        }
    }

    public virtual async Task InsertBulkEdit(IEnumerable<BulkEditProductModel> products)
    {
        foreach (var pModel in products)
        {
            if (string.IsNullOrWhiteSpace(pModel.Name))
                continue;

            //the defaults and the path of the Create screen: layout, tax category, store and
            //vendor ownership and the search engine name come out the same as there
            var model = new ProductModel { StoreId = scope.DefaultStoreId };
            await PrepareProductModel(model, null, true, true);
            model.ProductTypeId = (int)ProductType.SimpleProduct;
            model.ProductLayoutId = model.AvailableProductLayouts.FirstOrDefault()?.Value;
            model.Name = pModel.Name.Trim();
            model.Sku = pModel.Sku;
            model.Price = pModel.Price;
            model.OldPrice = pModel.OldPrice;
            model.StockQuantity = pModel.StockQuantity;
            model.ManageInventoryMethodId = pModel.ManageInventoryMethodId;
            model.Published = pModel.Published;
            if (scope.DefaultStoreId is not null)
                model.Stores = [scope.DefaultStoreId];

            await InsertProductModel(model);
        }
    }

    public virtual async Task DeleteBulkEdit(IEnumerable<BulkEditProductModel> products)
    {
        foreach (var pModel in products)
        {
            //delete
            var product = await productService.GetProductById(pModel.Id, true);
            if (product != null)
            {
                await productService.DeleteProduct(product);
            }
        }
    }

    public virtual async Task<ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel> PrepareAssociateProductToAttributeValueModel()
    {
        var model = await PrepareAddProductModel<ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel>();
        return model;
    }

    protected virtual async Task UpdatePictureSeoNames(Product product)
    {
        var picturesename = pictureService.GetPictureSeName(product.Name);
        foreach (var pp in product.ProductPictures)
        {
            var picture = await pictureService.GetPictureById(pp.PictureId);
            if (picture != null)
                await pictureService.SetSeoFilename(picture, picturesename);
        }
    }

    protected virtual async Task<List<string>> GetChildCategoryIds(string parentCategoryId)
    {
        var categoriesIds = new List<string>();
        var categories = await categoryService.GetAllCategoriesByParentCategoryId(
            access: CatalogAccess.IncludingHidden(), parentCategoryId: parentCategoryId);
        foreach (var category in categories)
        {
            categoriesIds.Add(category.Id);
            categoriesIds.AddRange(await GetChildCategoryIds(category.Id));
        }

        return categoriesIds;
    }

    protected virtual string[] ParseProductTags(string productTags)
    {
        var result = new List<string>();
        if (!string.IsNullOrWhiteSpace(productTags))
        {
            var values = productTags.Split([','], StringSplitOptions.RemoveEmptyEntries);
            foreach (var val1 in values)
                if (!string.IsNullOrEmpty(val1.Trim()))
                    result.Add(val1.Trim());
        }

        return result.ToArray();
    }

    protected virtual async Task SaveProductTags(Product product, string[] productTags)
    {
        ArgumentNullException.ThrowIfNull(product);

        //product tags
        var existingProductTags = product.ProductTags.ToList();
        var productTagsToRemove = new List<ProductTag>();
        foreach (var existingProductTag in existingProductTags)
        {
            var existingProductTagText =
                await productTagService.GetProductTagByName(existingProductTag.ToLowerInvariant());
            var found = false;
            foreach (var newProductTag in productTags)
                if (existingProductTagText != null)
                    if (existingProductTagText.Name.Equals(newProductTag, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }

            if (!found) productTagsToRemove.Add(existingProductTagText);
        }

        foreach (var productTag in productTagsToRemove)
            if (productTag != null)
                await productTagService.DetachProductTag(productTag, product.Id);
        foreach (var productTagName in productTags)
        {
            ProductTag productTag;
            var productTag2 = await productTagService.GetProductTagByName(productTagName);
            if (productTag2 == null)
            {
                //add new product tag
                productTag = new ProductTag {
                    Name = productTagName,
                    SeName = SeoExtensions.GetSeName(productTagName, seoSettings.ConvertNonWesternChars,
                        seoSettings.AllowUnicodeCharsInUrls, seoSettings.SeoCharConversion),
                    Count = 0
                };
                await productTagService.InsertProductTag(productTag);
            }
            else
            {
                productTag = productTag2;
            }

            if (!product.ProductTagExists(productTag.Name))
                await productTagService.AttachProductTag(productTag, product.Id);
        }
    }

    protected virtual async Task<T> PrepareAddProductModel<T>() where T : ProductModel.AddProductModel, new()
    {
        var storeId = scope.DefaultStoreId ?? "";
        var model = new T();

        //stores - same capability gate as PrepareProductListModel: Vendor's original PrepareAddProductModel<T>
        //never populated a stores dropdown at all (vendors don't pick stores), so this must not run for
        //Vendor even though its storeId also resolves to "".
        if (scope.ShowStoreSelector)
        {
            model.AvailableStores.Add(new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var s in (await storeService.GetAllStores()).Where(x =>
                         x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });
        }

        //vendors - only when the host isn't already forced onto a single vendor (scope.DefaultVendorId):
        //Vendor's original PrepareAddProductModel<T> never populated a vendor picker at all (matches the
        //ShowStoreSelector gate above for the same reason - there's nothing to pick, the search is always
        //forced to the current vendor).
        if (scope.DefaultVendorId is null)
        {
            model.AvailableVendors.Add(new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var v in await vendorService.GetAllVendors(showHidden: true))
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id });
        }

        //product types
        model.AvailableProductTypes = enumTranslationService.ToSelectList(ProductType.SimpleProduct, false).ToList();
        model.AvailableProductTypes.Insert(0,
            new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = "0" });

        return model;
    }
}