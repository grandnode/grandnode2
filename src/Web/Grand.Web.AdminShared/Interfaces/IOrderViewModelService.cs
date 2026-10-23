using Grand.Domain.Orders;
using Grand.Web.AdminShared.Models.Orders;

namespace Grand.Web.AdminShared.Interfaces;

public interface IOrderViewModelService
{
    Task<OrderListModel> PrepareOrderListModel(int? orderStatusId = null, int? paymentStatusId = null,
        int? shippingStatusId = null, DateTime? startDate = null, string storeId = null, string code = null);

    Task<(IEnumerable<OrderModel> orderModels, int totalCount)> PrepareOrderModel(OrderListModel model, int pageIndex,
        int pageSize);

    Task PrepareOrderDetailsModel(OrderModel model, Order order);
    Task<IList<Order>> PrepareOrders(OrderListModel model);
    Task SaveOrderTags(Order order, string tags);
}