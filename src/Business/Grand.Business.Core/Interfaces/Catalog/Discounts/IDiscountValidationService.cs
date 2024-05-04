using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Catalog.Discounts;

public interface IDiscountValidationService
{
    /// <summary>
    ///     Validates used discount
    /// </summary>
    /// <param name="discount">Discount</param>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">Currency</param>
    /// <returns>Discount validation result</returns>
    Task<DiscountValidationResult> ValidateDiscount(Discount discount, Customer customer, Store store,
        Currency currency);

    /// <summary>
    ///     Validates used discount with coupon code
    /// </summary>
    /// <param name="discount">Discount</param>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">Currency</param>
    /// <param name="couponCodeToValidate">Coupon code that should be validated</param>
    /// <returns>Discount validation result</returns>
    Task<DiscountValidationResult> ValidateDiscount(Discount discount, Customer customer, Store store,
        Currency currency, string couponCodeToValidate);

    /// <summary>
    ///     Validate discount
    /// </summary>
    /// <param name="discount">Discount</param>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">Currency</param>
    /// <param name="couponCodesToValidate">Coupon codes that should be validated</param>
    /// <returns>Discount validation result</returns>
    Task<DiscountValidationResult> ValidateDiscount(Discount discount, Customer customer, Store store,
        Currency currency, string[] couponCodesToValidate);

    /// <summary>
    ///     Exist coupon code in discount
    /// </summary>
    /// <param name="couponCode"></param>
    /// <param name="discountId"></param>
    /// <param name="used"></param>
    /// <returns></returns>
    Task<bool> ExistsCodeInDiscount(string couponCode, string discountId, bool? used);
}