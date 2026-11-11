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
    Task NotifyCustomer(MerchandiseReturn merchandiseReturn);
    MerchandiseReturnListModel PrepareReturnRequestListModel();

    Task<IList<MerchandiseReturnModel.MerchandiseReturnItemModel>> PrepareMerchandiseReturnItemModel(
        string merchandiseReturnId);

    /// <summary>
    ///     Saves the edit form. When the status change is refused nothing is saved, and Error is the reason as a
    ///     resource key without the panel's prefix
    /// </summary>
    Task<(MerchandiseReturn MerchandiseReturn, string Error)> UpdateMerchandiseReturnModel(MerchandiseReturn merchandiseReturn,
        MerchandiseReturnModel model, List<CustomAttribute> customAddressAttributes);

    Task DeleteMerchandiseReturn(MerchandiseReturn merchandiseReturn);

    Task<IList<MerchandiseReturnModel.MerchandiseReturnNote>> PrepareMerchandiseReturnNotes(
        MerchandiseReturn merchandiseReturn);

    Task InsertMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, Order order, string downloadId,
        bool displayToCustomer, string message);

    Task DeleteMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, string id);
}