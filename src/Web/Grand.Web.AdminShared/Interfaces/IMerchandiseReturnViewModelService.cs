using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Orders;

namespace Grand.Web.AdminShared.Interfaces;

public interface IMerchandiseReturnViewModelService
{
    Task<MerchandiseReturnModel> PrepareMerchandiseReturnModel(MerchandiseReturnModel model,
        MerchandiseReturn merchandiseReturn, bool excludeProperties);

    Task<(IList<MerchandiseReturnModel> merchandiseReturnModels, int totalCount)> PrepareMerchandiseReturnModel(
        MerchandiseReturnListModel model, int pageIndex, int pageSize);

    Task<AddressModel> PrepareAddressModel(AddressModel model, Address address, bool excludeProperties);
    MerchandiseReturnListModel PrepareReturnRequestListModel();

    Task<IList<MerchandiseReturnModel.MerchandiseReturnItemModel>> PrepareMerchandiseReturnItemModel(
        string merchandiseReturnId);

    /// <summary>
    ///     The return receipts of the return and whether "Receive to stock" is offered; only for a panel with
    ///     inventory documents, as it reads the panel's warehouses
    /// </summary>
    Task PrepareReturnReceipts(MerchandiseReturnModel model);

    /// <summary>
    ///     The refund of the return: the refund made or under way, in any panel; or, for a panel that refunds
    ///     (canRefund) and a staff member who refunds payments, what the refund of a received return comes to, with
    ///     the amount to refund filled in
    /// </summary>
    Task PrepareRefund(MerchandiseReturnModel model, MerchandiseReturn merchandiseReturn, bool canRefund);

    /// <summary>
    ///     Gives back the amount for the return through the payment of its order, online or offline; the reason it
    ///     was not, as a resource key without the panel's prefix, or null
    /// </summary>
    Task<string> RefundMerchandiseReturn(MerchandiseReturn merchandiseReturn, double amount, bool offline);

    /// <summary>
    ///     Saves the edit form, then changes the status when a status button posted it (ChangeStatusTo). Error is
    ///     why the form or the status was not saved, as a resource key without the panel's prefix
    /// </summary>
    Task<(MerchandiseReturn MerchandiseReturn, string Error)> UpdateMerchandiseReturnModel(MerchandiseReturn merchandiseReturn,
        MerchandiseReturnModel model, List<CustomAttribute> customAddressAttributes);

    /// <summary>
    ///     Deletes the return; the reason it was not, as a resource key without the panel's prefix, or null
    /// </summary>
    Task<string> DeleteMerchandiseReturn(MerchandiseReturn merchandiseReturn);

    Task<IList<MerchandiseReturnModel.MerchandiseReturnNote>> PrepareMerchandiseReturnNotes(
        MerchandiseReturn merchandiseReturn);

    Task InsertMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, Order order, string downloadId,
        bool displayToCustomer, string message);

    Task DeleteMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, string id);
}