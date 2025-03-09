using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Stores;

namespace Grand.Business.Catalog.Services.Discounts;

public class DiscountHandlerService : IDiscountHandlerService
{
    private readonly IDiscountService _discountService;
    private readonly ICategoryService _categoryService;
    private readonly IBrandService _brandService;
    private readonly ICollectionService _collectionService;
    private readonly IVendorService _vendorService;
    private readonly IDiscountValidationService _discountValidationService;
    private readonly CatalogSettings _catalogSettings;

    public DiscountHandlerService(
        IDiscountService discountService,
        ICategoryService categoryService,
        IBrandService brandService,
        ICollectionService collectionService,
        IVendorService vendorService,
        IDiscountValidationService discountValidationService,
        CatalogSettings catalogSettings)
    {
        _discountService = discountService;
        _categoryService = categoryService;
        _brandService = brandService;
        _collectionService = collectionService;
        _vendorService = vendorService;
        _discountValidationService = discountValidationService;
        _catalogSettings = catalogSettings;
    }

    /// <summary>
    /// Gets allowed discounts
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">Currency</param>
    /// <returns>Discounts</returns>
    private async Task AddAllowedDiscounts(IList<ApplyDiscount> allowedDiscounts, IEnumerable<string> appliedDiscounts, Customer customer, Store store, Currency currency, DiscountType discountType)
    {
        foreach (var appliedDiscount in appliedDiscounts)
        {
            var discount = await _discountService.GetDiscountById(appliedDiscount);
            if (discount == null) continue;
            var validDiscount = await _discountValidationService.ValidateDiscount(discount, customer, store, currency);
            if (validDiscount.IsValid && discount.DiscountTypeId == discountType)
                allowedDiscounts.Add(new ApplyDiscount {
                    CouponCode = validDiscount.CouponCode,
                    DiscountId = discount.Id,
                    IsCumulative = discount.IsCumulative,
                    MaximumDiscountedQuantity = discount.MaximumDiscountedQuantity
                });
        }
    }

    public virtual async Task<IList<ApplyDiscount>> GetAllowedDiscounts(Product product, Customer customer, Store store, Currency currency)
    {
        var allowedDiscounts = new List<ApplyDiscount>();
        if (_catalogSettings.IgnoreDiscounts)
            return allowedDiscounts;

        //discounts applied to products
        await AddAllowedDiscounts(allowedDiscounts, product.AppliedDiscounts, customer, store, currency, DiscountType.AssignedToSkus);

        //discounts applied to all products
        var allProductDiscounts = await _discountService.GetActiveDiscountsByContext(DiscountType.AssignedToAllProducts, store.Id, currency.CurrencyCode);
        await AddAllowedDiscounts(allowedDiscounts, allProductDiscounts.Select(d => d.Id), customer, store, currency, DiscountType.AssignedToAllProducts);

        //discounts applied to categories
        foreach (var productCategory in product.ProductCategories)
        {
            var category = await _categoryService.GetCategoryById(productCategory.CategoryId);
            if (category == null) continue;
            await AddAllowedDiscounts(allowedDiscounts, category.AppliedDiscounts, customer, store, currency, DiscountType.AssignedToCategories);
        }

        //discounts applied to brands
        if (!string.IsNullOrEmpty(product.BrandId))
        {
            var brand = await _brandService.GetBrandById(product.BrandId);
            if (brand != null)
                await AddAllowedDiscounts(allowedDiscounts, brand.AppliedDiscounts, customer, store, currency, DiscountType.AssignedToBrands);
        }

        //discounts applied to collections
        foreach (var productCollection in product.ProductCollections)
        {
            var collection = await _collectionService.GetCollectionById(productCollection.CollectionId);
            if (collection == null) continue;
            await AddAllowedDiscounts(allowedDiscounts, collection.AppliedDiscounts, customer, store, currency, DiscountType.AssignedToCollections);
        }

        //discounts applied to vendors
        if (!string.IsNullOrEmpty(product.VendorId))
        {
            var vendor = await _vendorService.GetVendorById(product.VendorId);
            if (vendor != null)
                await AddAllowedDiscounts(allowedDiscounts, vendor.AppliedDiscounts, customer, store, currency, DiscountType.AssignedToVendors);
        }

        return FilterDiscountsByMaxQuantity(allowedDiscounts);
    }

    private static IList<ApplyDiscount> FilterDiscountsByMaxQuantity(IList<ApplyDiscount> discounts)
    {
        if (!discounts.Any())
            return discounts;

        // Identify discounts that are cumulative and have a MaximumDiscountedQuantity > 0
        var cumulativeDiscounts = discounts
            .Where(d => d.IsCumulative && (d.MaximumDiscountedQuantity ?? 0) > 0)
            .ToList();

        if (!cumulativeDiscounts.Any())
            return discounts;

        // Determine the maximum MaximumDiscountedQuantity among cumulative discounts
        var maxQuantity = cumulativeDiscounts.Max(d => d.MaximumDiscountedQuantity ?? 0);

        // Select cumulative discounts with the maximum MaximumDiscountedQuantity
        var maxCumulativeDiscounts = cumulativeDiscounts
            .Where(d => d.MaximumDiscountedQuantity == maxQuantity)
            .ToList();

        // Select discounts that are not cumulative or have MaximumDiscountedQuantity <= 0
        var otherDiscounts = discounts
            .Where(d => !(d.IsCumulative && (d.MaximumDiscountedQuantity ?? 0) > 0))
            .ToList();

        // Combine the filtered cumulative discounts with the other discounts
        return maxCumulativeDiscounts.Concat(otherDiscounts).ToList();
    }

    public async Task<(List<ApplyDiscount> appliedDiscount, double discountAmount)> GetDiscountAmount(Product product, Customer customer, Store store, Currency currency, double productPriceWithoutDiscount)
    {
        var allowedDiscounts = await GetAllowedDiscounts(product, customer, store, currency);

        //no discounts
        if (!allowedDiscounts.Any())
            return ([], 0);

        var preferredDiscount = await _discountService.GetPreferredDiscount(allowedDiscounts, customer, currency,
            product, productPriceWithoutDiscount);

        return (preferredDiscount.appliedDiscount, preferredDiscount.discountAmount);
    }
}