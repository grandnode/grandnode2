using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Order.ExternalOrderApi.Models;
using OrderEntity = Grand.Domain.Orders.Order;

namespace Order.ExternalOrderApi.Services;

public interface IExternalOrderService
{
    Task<(bool IsValid, IList<Grand.Domain.Catalog.Product> Products, IList<string> MissingSkus)> ValidateProducts(IList<string> skus);
    
    Task<Customer> GetOrCreateCustomer(ExternalOrderModel model);
 
    Task CreateShoppingCartItems(Customer customer, ExternalOrderModel model, IList<Grand.Domain.Catalog.Product> products);
    
    Task<OrderProcessResult> PlaceOrder(Customer customer);
   
    Task<OrderProcessResult> ProcessOrder(ExternalOrderModel model);
    
    Task SetPaymentMethod(Customer customer, string paymentMethodSystemName);
}
