using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;

namespace Grand.Business.Catalog.Services.Products;

/// <summary>
///     Copy Product service
/// </summary>
public class CopyProductService : ICopyProductService
{
    
    #region Fields

    private readonly IProductService _productService;
    private readonly ISlugService _slugService;
    private readonly ISeNameService _seNameService;
    private readonly IStockMovementService _stockMovementService;
    
    #endregion
    #region Ctor

    public CopyProductService(IProductService productService,
        ISlugService slugService,
        ISeNameService seNameService,
        IStockMovementService stockMovementService)
    {
        _productService = productService;
        _slugService = slugService;
        _seNameService = seNameService;
        _stockMovementService = stockMovementService;
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Create a copy of product with all depended data
    /// </summary>
    /// <param name="product">The product to copy</param>
    /// <param name="newName">The name of product duplicate</param>
    /// <param name="isPublished">A value indicating whether the product duplicate should be published</param>
    /// <param name="copyAssociatedProducts">A value indicating whether the copy associated products</param>
    /// <returns>Product copy</returns>
    public virtual async Task<Product> CopyProduct(Product product, string newName,
        bool isPublished = true, bool copyAssociatedProducts = true)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (string.IsNullOrEmpty(newName))
            newName = $"{product.Name} - CopyProduct";

        // product
        var productCopy = new Product {
            ProductTypeId = product.ProductTypeId,
            ParentGroupedProductId = product.ParentGroupedProductId,
            VisibleIndividually = product.VisibleIndividually,
            Name = newName,
            ShortDescription = product.ShortDescription,
            FullDescription = product.FullDescription,
            Flag = product.Flag,
            BrandId = product.BrandId,
            VendorId = product.VendorId,
            ProductLayoutId = product.ProductLayoutId,
            AdminComment = product.AdminComment,
            ShowOnHomePage = product.ShowOnHomePage,
            BestSeller = product.BestSeller,
            MetaKeywords = product.MetaKeywords,
            MetaDescription = product.MetaDescription,
            MetaTitle = product.MetaTitle,
            AllowCustomerReviews = product.AllowCustomerReviews,
            LimitedToStores = product.LimitedToStores,
            Sku = product.Sku,
            Mpn = product.Mpn,
            Gtin = product.Gtin,
            IsGiftVoucher = product.IsGiftVoucher,
            GiftVoucherTypeId = product.GiftVoucherTypeId,
            OverGiftAmount = product.OverGiftAmount,
            RequireOtherProducts = product.RequireOtherProducts,
            RequiredProductIds = product.RequiredProductIds,
            AutoAddRequiredProducts = product.AutoAddRequiredProducts,
            IsDownload = product.IsDownload,
            UnlimitedDownloads = product.UnlimitedDownloads,
            MaxNumberOfDownloads = product.MaxNumberOfDownloads,
            DownloadExpirationDays = product.DownloadExpirationDays,
            DownloadActivationTypeId = product.DownloadActivationTypeId,
            HasSampleDownload = product.HasSampleDownload,
            HasUserAgreement = product.HasUserAgreement,
            UserAgreementText = product.UserAgreementText,
            IsRecurring = product.IsRecurring,
            RecurringCycleLength = product.RecurringCycleLength,
            RecurringCyclePeriodId = product.RecurringCyclePeriodId,
            RecurringTotalCycles = product.RecurringTotalCycles,
            IsShipEnabled = product.IsShipEnabled,
            IsFreeShipping = product.IsFreeShipping,
            ShipSeparately = product.ShipSeparately,
            AdditionalShippingCharge = product.AdditionalShippingCharge,
            DeliveryDateId = product.DeliveryDateId,
            IsTaxExempt = product.IsTaxExempt,
            TaxCategoryId = product.TaxCategoryId,
            IsTele = product.IsTele,
            ManageInventoryMethodId = product.ManageInventoryMethodId,
            UseMultipleWarehouses = product.UseMultipleWarehouses,
            WarehouseId = product.WarehouseId,
            StockQuantity = product.StockQuantity,
            StockAvailability = product.StockAvailability,
            DisplayStockQuantity = product.DisplayStockQuantity,
            MinStockQuantity = product.MinStockQuantity,
            LowStock = product.MinStockQuantity > 0 && product.MinStockQuantity >= product.StockQuantity,
            LowStockActivityId = product.LowStockActivityId,
            NotifyAdminForQuantityBelow = product.NotifyAdminForQuantityBelow,
            BackorderModeId = product.BackorderModeId,
            AllowOutOfStockSubscriptions = product.AllowOutOfStockSubscriptions,
            OrderMinimumQuantity = product.OrderMinimumQuantity,
            OrderMaximumQuantity = product.OrderMaximumQuantity,
            AllowedQuantities = product.AllowedQuantities,
            DisableBuyButton = product.DisableBuyButton,
            DisableWishlistButton = product.DisableWishlistButton,
            AvailableForPreOrder = product.AvailableForPreOrder,
            PreOrderDateTimeUtc = product.PreOrderDateTimeUtc,
            CallForPrice = product.CallForPrice,
            Price = product.Price,
            OldPrice = product.OldPrice,
            CatalogPrice = product.CatalogPrice,
            StartPrice = product.StartPrice,
            ProductCost = product.ProductCost,
            EnteredPrice = product.EnteredPrice,
            MinEnteredPrice = product.MinEnteredPrice,
            MaxEnteredPrice = product.MaxEnteredPrice,
            BasepriceEnabled = product.BasepriceEnabled,
            BasepriceAmount = product.BasepriceAmount,
            BasepriceUnitId = product.BasepriceUnitId,
            BasepriceBaseAmount = product.BasepriceBaseAmount,
            BasepriceBaseUnitId = product.BasepriceBaseUnitId,
            MarkAsNew = product.MarkAsNew,
            MarkAsNewStartDateTimeUtc = product.MarkAsNewStartDateTimeUtc,
            MarkAsNewEndDateTimeUtc = product.MarkAsNewEndDateTimeUtc,
            Weight = product.Weight,
            Length = product.Length,
            Width = product.Width,
            Height = product.Height,
            AvailableStartDateTimeUtc = product.AvailableStartDateTimeUtc,
            AvailableEndDateTimeUtc = product.AvailableEndDateTimeUtc,
            DisplayOrder = product.DisplayOrder,
            Published = isPublished,
            Locales = product.Locales,
            CustomerGroups = product.CustomerGroups,
            Stores = product.Stores
        };

        // product <-> warehouses mappings
        foreach (var pwi in product.ProductWarehouseInventory) productCopy.ProductWarehouseInventory.Add(pwi);

        // product <-> categories mappings
        foreach (var productCategory in product.ProductCategories) productCopy.ProductCategories.Add(productCategory);

        // product <-> collections mappings
        foreach (var productCollections in product.ProductCollections)
            productCopy.ProductCollections.Add(productCollections);

        // product <-> related products mappings
        foreach (var relatedProduct in product.RelatedProducts) productCopy.RelatedProducts.Add(relatedProduct);

        //product tags
        foreach (var productTag in product.ProductTags) productCopy.ProductTags.Add(productTag);

        // product <-> attributes mappings
        foreach (var productAttributeMapping in product.ProductAttributeMappings)
            productCopy.ProductAttributeMappings.Add(productAttributeMapping);
        //attribute combinations
        foreach (var combination in product.ProductAttributeCombinations)
            productCopy.ProductAttributeCombinations.Add(combination);

        foreach (var csProduct in product.CrossSellProduct) productCopy.CrossSellProduct.Add(csProduct);

        foreach (var reProduct in product.RecommendedProduct) productCopy.RecommendedProduct.Add(reProduct);

        // product specifications
        foreach (var productSpecificationAttribute in product.ProductSpecificationAttributes)
            productCopy.ProductSpecificationAttributes.Add(productSpecificationAttribute);

        //tier prices
        foreach (var tierPrice in product.TierPrices) productCopy.TierPrices.Add(tierPrice);

        // product <-> discounts mapping
        foreach (var discount in product.AppliedDiscounts) productCopy.AppliedDiscounts.Add(discount);

        productCopy.InventoryTrackingModeId = product.InventoryTrackingModeId;
        productCopy.WarrantyMonths = product.WarrantyMonths;
        productCopy.ExpiryWarningDays = product.ExpiryWarningDays;
        //batches and serial numbers are physical units: a copy of a product tracking them starts with no stock
        if (InventoryTracking.Tracked(product))
            WithoutStock(productCopy);

        //the copy starts with the source's stock: its journal says where the stock came from
        await _stockMovementService.JournalNewProduct(productCopy, InventoryMovementType.ProductCopy,
            new StockSource(nameof(Product), product.Id, Reference: product.Name));

        //validate search engine name
        await _productService.InsertProduct(productCopy);

        //search engine name
        var seName = await _seNameService.ValidateSeName(productCopy, "", productCopy.Name, true);
        productCopy.SeName = seName;
        await _productService.UpdateProduct(productCopy);
        await _slugService.SaveSlug(productCopy, seName, "");

        return productCopy;
    }

    #endregion

    /// <summary>
    ///     Empties the stock of a copy: its warehouse rows and combinations are its own, without stock
    /// </summary>
    private static void WithoutStock(Product copy)
    {
        copy.StockQuantity = 0;
        copy.ReservedQuantity = 0;
        copy.LowStock = true;
        var rows = copy.ProductWarehouseInventory.Select(x => new ProductWarehouseInventory { WarehouseId = x.WarehouseId })
            .ToList();
        copy.ProductWarehouseInventory.Clear();
        foreach (var row in rows)
            copy.ProductWarehouseInventory.Add(row);

        var combinations = copy.ProductAttributeCombinations.Select(x => new ProductAttributeCombination {
            Id = x.Id,
            Attributes = x.Attributes,
            AllowOutOfStockOrders = x.AllowOutOfStockOrders,
            Text = x.Text,
            Sku = x.Sku,
            Mpn = x.Mpn,
            Gtin = x.Gtin,
            OverriddenPrice = x.OverriddenPrice,
            NotifyAdminForQuantityBelow = x.NotifyAdminForQuantityBelow,
            PictureId = x.PictureId
        }).ToList();
        foreach (var (combination, source) in combinations.Zip(copy.ProductAttributeCombinations))
        {
            foreach (var row in source.WarehouseInventory)
                combination.WarehouseInventory.Add(new ProductCombinationWarehouseInventory { WarehouseId = row.WarehouseId });
            foreach (var tierPrice in source.TierPrices)
                combination.TierPrices.Add(tierPrice);
        }
        copy.ProductAttributeCombinations.Clear();
        foreach (var combination in combinations)
            copy.ProductAttributeCombinations.Add(combination);
    }
}
