using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Web.Vendor.Models.MerchandiseReturn;

namespace Grand.Web.Vendor.Interfaces;

public interface IMerchandiseReturnViewModelService
{
    Task<MerchandiseReturnModel> PrepareMerchandiseReturnModel(MerchandiseReturnModel model,
        MerchandiseReturn merchandiseReturn, bool excludeProperties);

    Task<(IList<MerchandiseReturnModel> merchandiseReturnModels, int totalCount)> PrepareMerchandiseReturnModel(
        MerchandiseReturnListModel model, int pageIndex, int pageSize);

    MerchandiseReturnListModel PrepareReturnRequestListModel();

    Task<IList<MerchandiseReturnModel.MerchandiseReturnItemModel>> PrepareMerchandiseReturnItemModel(
        string merchandiseReturnId);

    Task<MerchandiseReturn> UpdateMerchandiseReturnModel(MerchandiseReturn merchandiseReturn,
        MerchandiseReturnModel model, List<CustomAttribute> customAddressAttributes);

    Task DeleteMerchandiseReturn(MerchandiseReturn merchandiseReturn);

    Task<IList<MerchandiseReturnModel.MerchandiseReturnNote>> PrepareMerchandiseReturnNotes(
        MerchandiseReturn merchandiseReturn);

    Task InsertMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, bool displayToCustomer, string message);
    Task DeleteMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, string id);
}