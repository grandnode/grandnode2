using Grand.Domain.Orders;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Messages;

public interface IMerchandiseReturnMessageProviderService
{
    /// <summary>
    ///     Sends 'New Merchandise Return' message to a store owner
    /// </summary>
    /// <param name="merchandiseReturn">Merchandise return</param>
    /// <param name="order">Order</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendNewMerchandiseReturnStoreOwnerMessage(MerchandiseReturn merchandiseReturn, Order order,
        string languageId, DomainHost host = null);

    /// <summary>
    ///     Sends 'Merchandise Return status changed' message to a customer
    /// </summary>
    /// <param name="merchandiseReturn">Merchandise return</param>
    /// <param name="order">Order item</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendMerchandiseReturnStatusChangedCustomerMessage(MerchandiseReturn merchandiseReturn, Order order,
        string languageId);

    /// <summary>
    ///     Sends 'New Merchandise Return' message to a customer
    /// </summary>
    /// <param name="merchandiseReturn"></param>
    /// <param name="order"></param>
    /// <param name="languageId"></param>
    /// <returns></returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendNewMerchandiseReturnCustomerMessage(MerchandiseReturn merchandiseReturn, Order order,
        string languageId, DomainHost host = null);

    /// <summary>
    ///     Sends a new merchandise return note added notification to a customer
    /// </summary>
    /// <param name="merchandiseReturn">Merchandise return</param>
    /// <param name="merchandiseReturnNote">Merchandise return note</param>
    /// <param name="order">Order</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendNewMerchandiseReturnNoteAddedCustomerMessage(MerchandiseReturn merchandiseReturn,
        MerchandiseReturnNote merchandiseReturnNote, Order order);
}
