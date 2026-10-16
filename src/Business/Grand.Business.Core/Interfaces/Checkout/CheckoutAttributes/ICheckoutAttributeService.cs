using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;

/// <summary>
///     Checkout attribute service
/// </summary>
public interface ICheckoutAttributeService
{
    #region Checkout attributes

    /// <summary>
    ///     Gets all checkout attributes
    /// </summary>
    /// <param name="access">Customer whose groups limit the result and the store, or every attribute of the store</param>
    /// <param name="excludeShippableAttributes">A value indicating whether we should exclude shippable attributes</param>
    /// <returns>Checkout attributes</returns>
    Task<IList<CheckoutAttribute>> GetAllCheckoutAttributes(CatalogAccess access,
        bool excludeShippableAttributes = false);

    /// <summary>
    ///     Gets a checkout attribute
    /// </summary>
    /// <param name="checkoutAttributeId">Checkout attribute identifier</param>
    /// <returns>Checkout attribute</returns>
    Task<CheckoutAttribute> GetCheckoutAttributeById(string checkoutAttributeId);

    /// <summary>
    ///     Inserts a checkout attribute
    /// </summary>
    /// <param name="checkoutAttribute">Checkout attribute</param>
    Task InsertCheckoutAttribute(CheckoutAttribute checkoutAttribute);

    /// <summary>
    ///     Updates the checkout attribute
    /// </summary>
    /// <param name="checkoutAttribute">Checkout attribute</param>
    Task UpdateCheckoutAttribute(CheckoutAttribute checkoutAttribute);

    /// <summary>
    ///     Deletes a checkout attribute
    /// </summary>
    /// <param name="checkoutAttribute">Checkout attribute</param>
    Task DeleteCheckoutAttribute(CheckoutAttribute checkoutAttribute);

    #endregion
}