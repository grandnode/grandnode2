using Grand.Domain.Customers;
using Grand.Web.AdminShared.Models.ShoppingCart;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICustomerShoppingCartViewModelService
{
    Task<IList<ShoppingCartItemModel>> PrepareShoppingCartItemModel(Customer customer, int cartTypeId);
    Task DeleteCart(Customer customer, string id);
    Task<IList<string>> UpdateCart(Customer customer, string shoppingCartId, double? unitprice);
}
