using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Products;

public class InsertProductReviewCommand : IRequest<ProductReview>
{
    public required Domain.Stores.Store Store { get; set; }
    public required Domain.Stores.DomainHost Host { get; set; }
    public required Customer Customer { get; set; }
    public required Product Product { get; set; }
    public required ProductReviewsModel Model { get; set; }
}