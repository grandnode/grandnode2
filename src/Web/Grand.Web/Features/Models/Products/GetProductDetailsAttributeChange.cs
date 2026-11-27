using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Stores;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Products;

public class GetProductDetailsAttributeChange : IRequest<ProductDetailsAttributeChangeModel>
{
    public required Customer Customer { get; set; }
    public required Currency Currency { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Product Product { get; set; }
    public required ProductModel Model { get; set; }
}