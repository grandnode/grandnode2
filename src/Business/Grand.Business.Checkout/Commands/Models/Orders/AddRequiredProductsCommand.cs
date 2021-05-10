using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Commands.Models.Orders
{
    public class AddRequiredProductsCommand : IRequest<bool>
    {
        public Customer Customer { get; set; }
        public ShoppingCartType ShoppingCartType { get; set; }
        public Product Product { get; set; }
        public string StoreId { get; set; }
    }
}
