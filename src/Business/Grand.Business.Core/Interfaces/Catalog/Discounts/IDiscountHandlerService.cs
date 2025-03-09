using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Catalog.Discounts;

public interface IDiscountHandlerService
{
    /// <summary>
    /// Gets allowed discounts for a product
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">Currency</param>
    /// <returns>Discounts</returns>
    Task<IList<ApplyDiscount>> GetAllowedDiscounts(Product product, Customer customer, Store store, Currency currency);

    /// <summary>
    /// Get discount amount
    /// </summary>
    /// <param name="product"></param>
    /// <param name="customer"></param>
    /// <param name="store"></param>
    /// <param name="currency"></param>
    /// <param name="productPriceWithoutDiscount"></param>
    /// <returns></returns>
    Task<(List<ApplyDiscount> appliedDiscount, double discountAmount)> GetDiscountAmount(Product product, Customer customer, Store store, Currency currency,
        double productPriceWithoutDiscount);
    
}