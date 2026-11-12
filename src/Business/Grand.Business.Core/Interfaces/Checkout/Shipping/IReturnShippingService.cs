using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Checkout.Shipping;

/// <summary>
///     The return shipping providers, and the options of a merchandise return the customer picks from
/// </summary>
public interface IReturnShippingService
{
    /// <summary>
    ///     Loads the providers active in the store and allowed for the customer, by priority
    /// </summary>
    Task<IList<IReturnShippingProvider>> LoadActiveProviders(Customer customer, string storeId);

    /// <summary>
    ///     Loads a provider by system name
    /// </summary>
    IReturnShippingProvider LoadProviderBySystemName(string systemName);

    /// <summary>
    ///     Loads the providers allowed in the store and for the customer, active or not, by priority
    /// </summary>
    IList<IReturnShippingProvider> LoadAllProviders(Customer customer = null, string storeId = "");

    /// <summary>
    ///     Gets the options offered for every vendor of the submitted items, with what the customer pays for all
    ///     parcels; none when no item is submitted
    /// </summary>
    Task<IList<ReturnShippingOffer>> GetOffers(MerchandiseReturnSubmitRequest request);

    /// <summary>
    ///     Validates the choice for every vendor of the submitted items; the keys of ByVendor are those of the returns
    ///     a submit creates
    /// </summary>
    Task<ReturnShippingResolution> Resolve(MerchandiseReturnSubmitRequest request, ReturnShippingChoice choice);

    /// <summary>
    ///     Gets the address the items of a vendor (empty for the store) are sent back to
    /// </summary>
    Task<Address> GetReturnAddress(string storeId, string vendorId);
}
