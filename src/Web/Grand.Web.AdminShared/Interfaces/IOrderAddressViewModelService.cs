using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Models.Orders;

namespace Grand.Web.AdminShared.Interfaces;

public interface IOrderAddressViewModelService
{
    Task<OrderAddressModel> PrepareOrderAddressModel(Order order, Address address);

    Task<Address> UpdateOrderAddress(Order order, Address address, OrderAddressModel model,
        List<CustomAttribute> customAttributes);
}
