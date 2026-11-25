using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class AddRequiredProductsCommand : IRequest<bool>
{
    public required Customer Customer { get; set; }
    public ShoppingCartType ShoppingCartType { get; set; }
    public required Product Product { get; set; }
    public required string StoreId { get; set; }

    /// <summary>
    ///     Language the cart warnings are written in
    /// </summary>
    public required Language Language { get; set; }
}