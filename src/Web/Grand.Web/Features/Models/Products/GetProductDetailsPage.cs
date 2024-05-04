using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Models.Products;

public class GetProductDetailsPage : IRequest<ProductDetailsModel>
{
    public Store Store { get; set; }
    public Product Product { get; set; }
    public ShoppingCartItem UpdateCartItem { get; set; }
    public bool IsAssociatedProduct { get; set; }
}