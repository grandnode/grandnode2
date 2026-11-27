using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Products;

public class GetProductReviewOverview : IRequest<ProductReviewOverviewModel>
{
    public required Product Product { get; set; }
    public required Language Language { get; set; }
    public required Domain.Stores.Store Store { get; set; }
}