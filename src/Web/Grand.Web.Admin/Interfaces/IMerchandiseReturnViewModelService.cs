using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Admin.Models.Orders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Interfaces
{
    public interface IMerchandiseReturnViewModelService
    {
        Task<MerchandiseReturnModel> PrepareMerchandiseReturnModel(MerchandiseReturnModel model,
            MerchandiseReturn merchandiseReturn, bool excludeProperties);
        Task<(IList<MerchandiseReturnModel> merchandiseReturnModels, int totalCount)> PrepareMerchandiseReturnModel(MerchandiseReturnListModel model, int pageIndex, int pageSize);
        Task<AddressModel> PrepareAddressModel(AddressModel model, Address address, bool excludeProperties);
        Task NotifyCustomer(MerchandiseReturn merchandiseReturn);
        MerchandiseReturnListModel PrepareReturnReqestListModel();
        Task<IList<MerchandiseReturnModel.MerchandiseReturnItemModel>> PrepareMerchandiseReturnItemModel(string merchandiseReturnId);
        Task<MerchandiseReturn> UpdateMerchandiseReturnModel(MerchandiseReturn merchandiseReturn, MerchandiseReturnModel model, List<CustomAttribute> customAddressAttributes);
        Task DeleteMerchandiseReturn(MerchandiseReturn merchandiseReturn);
        Task<IList<MerchandiseReturnModel.MerchandiseReturnNote>> PrepareMerchandiseReturnNotes(MerchandiseReturn merchandiseReturn);
        Task InsertMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, Order order, string downloadId, bool displayToCustomer, string message);
        Task DeleteMerchandiseReturnNote(MerchandiseReturn merchandiseReturn, string id);
    }
}
