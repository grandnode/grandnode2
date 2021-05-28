using Grand.Business.Catalog.Utilities;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Discounts
{
    /// <summary>
    /// Discount service interface
    /// </summary>
    public partial interface IDiscountService
    {
        
        /// <summary>
        /// Gets a discount
        /// </summary>
        /// <param name="discountId">Discount id</param>
        /// <returns>Discount</returns>
        Task<Discount> GetDiscountById(string discountId);

        /// <summary>
        /// Gets all discounts
        /// </summary>
        /// <returns>Discounts</returns>
        Task<IList<Discount>> GetAllDiscounts(DiscountType? discountType, string storeId = "", string currencyCode = "",
            string couponCode = "", string discountName = "", bool showHidden = false);

        /// <summary>
        /// Inserts a new discount
        /// </summary>
        /// <param name="discount">Discount</param>
        Task InsertDiscount(Discount discount);

        /// <summary>
        /// Updates the existing discount
        /// </summary>
        /// <param name="discount">Discount</param>
        Task UpdateDiscount(Discount discount);

        /// <summary>
        /// Deletes the existing discount
        /// </summary>
        /// <param name="discount">Discount</param>
        Task DeleteDiscount(Discount discount);

        /// <summary>
        /// Deletes the existing discount requirement
        /// </summary>
        /// <param name="discountRequirement">Discount requirement</param>
        Task DeleteDiscountRequirement(DiscountRule discountRequirement);

        /// <summary>
        /// Loads existing discount provider by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Discount provider</returns>
        IDiscountProvider LoadDiscountProviderBySystemName(string systemName);

        /// <summary>
        /// Loads all available discount providers
        /// </summary>
        /// <returns>Discount requirement rules</returns>
        IList<IDiscountProvider> LoadAllDiscountProviders();


        /// <summary>
        /// Gets existing discount by coupon code
        /// </summary>
        /// <param name="couponCode">CouponCode</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Discount</returns>
        Task<Discount> GetDiscountByCouponCode(string couponCode, bool showHidden = false);

        /// <summary>
        /// Exists coupon code in discount
        /// </summary>
        /// <param name="couponCode"></param>
        /// <param name="discountId"></param>
        /// <returns></returns>
        Task<bool> ExistsCodeInDiscount(string couponCode, string discountId, bool? used);

        /// <summary>
        /// Validates used discount
        /// </summary>
        /// <param name="discount">Discount</param>
        /// <param name="customer">Customer</param>
        /// <param name="currency">Currency</param>
        /// <returns>Discount validation result</returns>
        Task<DiscountValidationResult> ValidateDiscount(Discount discount, Customer customer, Currency currency);

        /// <summary>
        /// Validates used discount with coupon code
        /// </summary>
        /// <param name="discount">Discount</param>
        /// <param name="customer">Customer</param>
        /// <param name="currency">Currency</param>
        /// <param name="couponCodeToValidate">Coupon code that should be validated</param>
        /// <returns>Discount validation result</returns>
        Task<DiscountValidationResult> ValidateDiscount(Discount discount, Customer customer, Currency currency, string couponCodeToValidate);

        /// <summary>
        /// Validate discount
        /// </summary>
        /// <param name="discount">Discount</param>
        /// <param name="customer">Customer</param>
        /// <param name="currency">Currency</param>
        /// <param name="couponCodesToValidate">Coupon codes that should be validated</param>
        /// <returns>Discount validation result</returns>
        Task<DiscountValidationResult> ValidateDiscount(Discount discount, Customer customer, Currency currency, string[] couponCodesToValidate);


        /// <summary>
        /// Gets a discount usage history item
        /// </summary>
        /// <param name="discountUsageHistoryId">Discount usage history item id</param>
        /// <returns>Discount usage history</returns>
        Task<DiscountUsageHistory> GetDiscountUsageHistoryById(string discountUsageHistoryId);

        /// <summary>
        /// Gets all discount usage history records
        /// </summary>
        /// <param name="discountId">Discount id; use null to load all existing discounts</param>
        /// <param name="customerId">Customer id; use null to load all existing customers</param>
        /// <param name="orderId">Order id; use null to load all existing orders</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Discount usage history records</returns>
        Task<IPagedList<DiscountUsageHistory>> GetAllDiscountUsageHistory(string discountId = "",
            string customerId = "", string orderId = "", bool? canceled = null,
            int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Inserts discount usage history item
        /// </summary>
        /// <param name="discountUsageHistory">Discount usage history item</param>
        Task InsertDiscountUsageHistory(DiscountUsageHistory discountUsageHistory);

        /// <summary>
        /// Updates discount usage history item
        /// </summary>
        /// <param name="discountUsageHistory">Discount usage history item</param>
        Task UpdateDiscountUsageHistory(DiscountUsageHistory discountUsageHistory);

        /// <summary>
        /// Deletes discount usage history item
        /// </summary>
        /// <param name="discountUsageHistory">Discount usage history item</param>
        Task DeleteDiscountUsageHistory(DiscountUsageHistory discountUsageHistory);

        /// <summary>
        /// Gets all existing coupon codes for discount
        /// </summary>
        /// <param name="discountId"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<IPagedList<DiscountCoupon>> GetAllCouponCodesByDiscountId(string discountId, int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets the discount code by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<DiscountCoupon> GetDiscountCodeById(string id);

        /// <summary>
        /// Gets the discount code by code
        /// </summary>
        /// <param name="couponCode"></param>
        /// <returns></returns>
        Task<DiscountCoupon> GetDiscountCodeByCode(string couponCode);

        /// <summary>
        /// Deletes discount code
        /// </summary>
        /// <param name="coupon"></param>
        Task DeleteDiscountCoupon(DiscountCoupon coupon);

        /// <summary>
        /// Updates discount code - defines it as used or not
        /// </summary>
        /// <param name="couponCode"></param>
        /// <param name="discountId"></param>
        /// <param name="used"></param>
        Task DiscountCouponSetAsUsed(string couponCode, bool used);

        /// <summary>
        /// Cancels discount if order was canceled or deleted
        /// </summary>
        /// <param name="orderId"></param>
        Task CancelDiscount(string orderId);

        /// <summary>
        /// Inserts discount code
        /// </summary>
        /// <param name="coupon"></param>
        Task InsertDiscountCoupon(DiscountCoupon coupon);

        /// <summary>
        /// Gets discount amount from provider
        /// </summary>
        /// <param name="discount">Discount</param>
        /// <param name="amount">Amount</param>
        /// <param name="currency">currency</param>
        /// <param name="customer">Customer</param>
        /// <param name="product">Product</param>
        /// <returns></returns>
        Task<double> GetDiscountAmount(Discount discount, Customer customer, Currency currency, Product product, double amount);

        /// <summary>
        /// Gets preferred discount
        /// </summary>
        /// <param name="discounts"></param>
        /// <param name="customer"></param>
        /// <param name="product"></param>
        /// <param name="currency"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        Task<(List<ApplyDiscount> appliedDiscount, double discountAmount)> GetPreferredDiscount(IList<ApplyDiscount> discounts,
            Customer customer, Currency currency, Product product, double amount);

        /// <summary>
        /// Get preferred discount
        /// </summary>
        /// <param name="discounts"></param>
        /// <param name="amount"></param>
        /// <param name="customer"></param>
        /// <param name="currency"></param>
        /// <returns>appliedDiscount and discountAmount</returns>
        Task<(List<ApplyDiscount> appliedDiscount, double discountAmount)> GetPreferredDiscount(IList<ApplyDiscount> discounts,
            Customer customer, Currency currency, double amount);

        /// <summary>
        /// GetDiscountAmountProvider
        /// </summary>
        /// <param name="discount"></param>
        /// <param name="customer"></param>
        /// <param name="product"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        Task<double> GetDiscountAmountProvider(Discount discount, Customer customer, Product product, double amount);

        /// <summary>
        /// Load discount amount provider by systemName
        /// </summary>
        /// <param name="systemName"></param>
        /// <returns></returns>
        IDiscountAmountProvider LoadDiscountAmountProviderBySystemName(string systemName);

        /// <summary>
        /// Load discount amount providers
        /// </summary>
        /// <returns></returns>
        IList<IDiscountAmountProvider> LoadDiscountAmountProviders();
    }
}
