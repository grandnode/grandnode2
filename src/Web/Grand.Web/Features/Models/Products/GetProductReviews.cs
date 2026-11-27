using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Products;

public class GetProductReviews : IRequest<ProductReviewsModel>
{
    public required Product Product { get; set; }
    public required Language Language { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Customer Customer { get; set; }
    public int Size { get; set; }
}