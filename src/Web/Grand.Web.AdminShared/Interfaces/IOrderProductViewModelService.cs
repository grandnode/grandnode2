using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Models.Orders;

namespace Grand.Web.AdminShared.Interfaces;

public interface IOrderProductViewModelService
{
    Task<OrderModel.AddOrderProductModel> PrepareAddOrderProductModel(Order order);

    Task<OrderModel.AddOrderProductModel.ProductDetailsModel> PrepareAddProductToOrderModel(Order order,
        Product product);

    Task<IList<string>> AddProductToOrderDetails(Order order, Product product, AddProductToOrderModel model);
}
